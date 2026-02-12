using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class KeysControllerTests
{
    private readonly Mock<IVaultService> _mockVaultService;
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly KeysController _controller;

    public KeysControllerTests()
    {
        _mockVaultService = new Mock<IVaultService>();
        var httpClient = new HttpClient();
        var mockApisixLogger = new Mock<ILogger<ApisixClient>>();
        _mockApisixClient = new Mock<ApisixClient>(httpClient, mockApisixLogger.Object);
        _controller = new KeysController(_mockVaultService.Object, _mockApisixClient.Object);
    }

    [Fact]
    public async Task CreateKey_ReturnsBadRequest_WhenOwnerIsEmpty()
    {
        var request = new CreateKeyRequest { Owner = "" };
        var result = await _controller.CreateKey(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateKey_ReturnsBadRequest_WhenRequestIsNull()
    {
        var result = await _controller.CreateKey(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RotateKey_DoesNotReturnFullKey()
    {
        var fakeKey = "abcdef1234567890abcdef1234567890"; // 32 chars
        _mockVaultService.Setup(v => v.RotateApiKeyAsync("test-consumer"))
            .ReturnsAsync(fakeKey);

        var result = await _controller.RotateKey("test-consumer");
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Verify the response doesn't contain the full key
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.DoesNotContain(fakeKey, json);
        // Key prefix should be first 8 chars + "..."
        Assert.Contains("abcdef12...", json);
    }

    [Fact]
    public async Task RotateKey_ReturnsBadRequest_WhenConsumerNotFound()
    {
        _mockVaultService.Setup(v => v.RotateApiKeyAsync("nonexistent"))
            .ThrowsAsync(new Exception("Consumer nonexistent not found in APISIX"));

        var result = await _controller.RotateKey("nonexistent");
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    }
}
