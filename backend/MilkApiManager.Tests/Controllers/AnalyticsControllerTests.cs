using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class AnalyticsControllerTests
{
    private readonly Mock<PrometheusService> _mockPrometheus;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        var httpClient = new HttpClient();
        var mockLogger = new Mock<ILogger<PrometheusService>>();
        _mockPrometheus = new Mock<PrometheusService>(httpClient, mockLogger.Object);
        _controller = new AnalyticsController(_mockPrometheus.Object);
    }

    [Theory]
    [InlineData("valid-consumer", "valid-route", true)]
    [InlineData("consumer_01", "route.v2", true)]
    [InlineData("", "", true)]            // Empty is allowed (no filter)
    [InlineData(null, null, true)]        // Null is allowed (no filter)
    [InlineData("consumer\"}", null, false)]   // PromQL injection attempt
    [InlineData(null, "route{}", false)]       // PromQL injection attempt
    [InlineData("a])}[5m", null, false)]       // PromQL injection attempt
    [InlineData("consumer with spaces", null, false)]
    public async Task GetRequests_ValidatesLabels(string? consumer, string? route, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            _mockPrometheus.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(new List<AnalyticsResult>());
        }

        var query = new AnalyticsQuery { Consumer = consumer, Route = route };
        var result = await _controller.GetRequests(query);

        if (shouldSucceed)
        {
            Assert.IsType<OkObjectResult>(result);
        }
        else
        {
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Theory]
    [InlineData("valid-consumer", true)]
    [InlineData("consumer\"}", false)]
    public async Task GetLatency_ValidatesLabels(string? consumer, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            _mockPrometheus.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(new List<AnalyticsResult>());
        }

        var query = new AnalyticsQuery { Consumer = consumer };
        var result = await _controller.GetLatency(query);

        if (shouldSucceed)
            Assert.IsType<OkObjectResult>(result);
        else
            Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("valid-consumer", true)]
    [InlineData("consumer\"}", false)]
    public async Task GetErrors_ValidatesLabels(string? consumer, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            _mockPrometheus.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(new List<AnalyticsResult>());
        }

        var query = new AnalyticsQuery { Consumer = consumer };
        var result = await _controller.GetErrors(query);

        if (shouldSucceed)
            Assert.IsType<OkObjectResult>(result);
        else
            Assert.IsType<BadRequestObjectResult>(result);
    }
}
