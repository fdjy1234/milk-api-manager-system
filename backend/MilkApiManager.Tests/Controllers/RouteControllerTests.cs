using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Services;
using Moq;
using MilkApiManager.Models.Apisix;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class RouteControllerTests
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<RouteController>> _mockLogger;
    private readonly RouteController _controller;

    public RouteControllerTests()
    {
        var httpClient = new HttpClient();
        var mockApisixLogger = new Mock<ILogger<ApisixClient>>();
        _mockApisixClient = new Mock<ApisixClient>(httpClient, mockApisixLogger.Object);
        _mockLogger = new Mock<ILogger<RouteController>>();
        _controller = new RouteController(_mockApisixClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetRoutes_ReturnsOk_WhenSuccessful()
    {
        _mockApisixClient.Setup(c => c.GetRoutesAsync()).ReturnsAsync("{\"list\":[]}");
        var result = await _controller.GetRoutes();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetRoutes_Returns500_WhenExceptionThrown()
    {
        _mockApisixClient.Setup(c => c.GetRoutesAsync()).ThrowsAsync(new Exception("connection failed"));
        var result = await _controller.GetRoutes();
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateRoute_ReturnsBadRequest_WhenConfigIsNull()
    {
        var result = await _controller.CreateRoute(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRoute_ReturnsBadRequest_WhenIdIsEmpty()
    {
        var route = new ApisixRoute { Id = "", Name = "test", Uri = "/test", ServiceId = "svc1" };
        var result = await _controller.CreateRoute(route);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRoute_ReturnsBadRequest_WhenConfigIsNull()
    {
        var result = await _controller.UpdateRoute("test-id", null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
