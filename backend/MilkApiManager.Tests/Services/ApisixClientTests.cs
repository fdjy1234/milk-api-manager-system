using Microsoft.Extensions.Logging;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MilkApiManager.Tests.Services;

/// <summary>
/// Tests for ApisixClient HTTP request-building and response-parsing logic.
/// Uses a MockHttpMessageHandler to intercept and verify outgoing HTTP calls.
/// </summary>
public class ApisixClientTests
{
    private readonly Mock<ILogger<ApisixClient>> _mockLogger;

    public ApisixClientTests()
    {
        _mockLogger = new Mock<ILogger<ApisixClient>>();
    }

    private ApisixClient CreateClientWithHandler(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new ApisixClient(httpClient, _mockLogger.Object);
    }

    // ─── CreateRequest: Header & Body ───────────────────────────

    [Fact]
    public async Task CreateRouteAsync_SendsPutRequest_WithAdminApiKey()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClientWithHandler(handler);
        var route = new Route { Id = "r1", Name = "test", Uri = "/test", ServiceId = "svc1" };
        await client.CreateRouteAsync("r1", route);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest!.Method);
        Assert.Contains("routes/r1", capturedRequest.RequestUri!.ToString());
        Assert.True(capturedRequest.Headers.Contains("X-API-KEY"));

        // Body should contain the serialized route
        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"name\":\"test\"", body);
        Assert.Contains("\"uri\":\"/test\"", body);
    }

    [Fact]
    public async Task GetRoutesAsync_SendsGetRequest_WithoutBody()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"list\":[]}", Encoding.UTF8, "application/json")
            });
        });

        var client = CreateClientWithHandler(handler);
        var result = await client.GetRoutesAsync();

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Null(capturedRequest.Content);
        Assert.Contains("routes", capturedRequest.RequestUri!.ToString());
        Assert.Equal("{\"list\":[]}", result);
    }

    // ─── Delete operations: fail silently ───────────────────────

    [Fact]
    public async Task DeleteRouteAsync_DoesNotThrow_WhenNotFound()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var client = CreateClientWithHandler(handler);
        // Should not throw
        await client.DeleteRouteAsync("nonexistent");
    }

    [Fact]
    public async Task DeleteConsumerAsync_DoesNotThrow_WhenServerError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = CreateClientWithHandler(handler);
        await client.DeleteConsumerAsync("no-user");
    }

    [Fact]
    public async Task DeleteServiceAsync_DoesNotThrow_WhenForbidden()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));

        var client = CreateClientWithHandler(handler);
        await client.DeleteServiceAsync("svc-x");
    }

    // ─── Create/Update: throws on failure ───────────────────────

    [Fact]
    public async Task CreateRouteAsync_ThrowsHttpRequestException_OnFailure()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        var client = CreateClientWithHandler(handler);
        var route = new Route { Id = "r1", Name = "t", Uri = "/t", ServiceId = "s" };

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.CreateRouteAsync("r1", route));
    }

    [Fact]
    public async Task UpdateConsumerAsync_ThrowsHttpRequestException_OnFailure()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = CreateClientWithHandler(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.UpdateConsumerAsync("user1", new { }));
    }

    // ─── GetBlacklistAsync: parsing logic ───────────────────────

    [Fact]
    public async Task GetBlacklistAsync_ReturnsEmptyList_WhenNotFound()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var client = CreateClientWithHandler(handler);
        var result = await client.GetBlacklistAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBlacklistAsync_ParsesBlacklist_WhenPresent()
    {
        var json = "{\"value\":{\"blacklist\":[\"10.0.0.1\",\"192.168.1.0/24\"]}}";
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }));

        var client = CreateClientWithHandler(handler);
        var result = await client.GetBlacklistAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains("10.0.0.1", result);
        Assert.Contains("192.168.1.0/24", result);
    }

    [Fact]
    public async Task GetBlacklistAsync_ReturnsEmptyList_WhenNoBlacklistProperty()
    {
        var json = "{\"value\":{\"other\":true}}";
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }));

        var client = CreateClientWithHandler(handler);
        var result = await client.GetBlacklistAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ─── UpdateBlacklistAsync ───────────────────────────────────

    [Fact]
    public async Task UpdateBlacklistAsync_SendsCorrectPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClientWithHandler(handler);
        await client.UpdateBlacklistAsync(new List<string> { "1.2.3.4" });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest!.Method);
        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"blacklist\"", body);
        Assert.Contains("1.2.3.4", body);
    }

    // ─── JSON serialization: null properties ignored ────────────

    [Fact]
    public async Task CreateRouteAsync_OmitsNullProperties_InSerialization()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClientWithHandler(handler);
        var route = new Route { Id = "r1", Name = "t", Uri = "/t", ServiceId = "s", Methods = null, Uris = null };
        await client.CreateRouteAsync("r1", route);

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.DoesNotContain("\"methods\"", body);
        Assert.DoesNotContain("\"uris\"", body);
    }

    // ─── GetRouteAsync: APISIX wrapper parsing ──────────────────

    [Fact]
    public async Task GetRouteAsync_ParsesNodeWrapper()
    {
        var apisixResponse = "{\"node\":{\"key\":\"/apisix/routes/r1\",\"value\":{\"name\":\"my-route\",\"uri\":\"/api\",\"service_id\":\"svc1\"}}}";
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(apisixResponse, Encoding.UTF8, "application/json")
            }));

        var client = CreateClientWithHandler(handler);
        var route = await client.GetRouteAsync("r1");

        Assert.NotNull(route);
        Assert.Equal("my-route", route!.Name);
        Assert.Equal("/api", route.Uri);
        Assert.Equal("svc1", route.ServiceId);
    }

    // ─── ConsumerGroup operations ───────────────────────────────

    [Fact]
    public async Task CreateConsumerGroupAsync_SendsCorrectPath()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClientWithHandler(handler);
        var group = new ConsumerGroup { Id = "g1", Plugins = null };
        await client.CreateConsumerGroupAsync("g1", group);

        Assert.Contains("consumer_groups/g1", capturedRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteConsumerGroupAsync_DoesNotThrow_OnFailure()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = CreateClientWithHandler(handler);
        await client.DeleteConsumerGroupAsync("g1");
    }
}

/// <summary>
/// Test double for HttpMessageHandler to intercept HTTP calls in unit tests.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}
