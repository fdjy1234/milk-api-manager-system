using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Services;
using Moq;
using System.Text.Json;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class ConsumerControllerTests
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<ConsumerController>> _mockLogger;
    private readonly ConsumerController _controller;

    public ConsumerControllerTests()
    {
        var httpClient = new HttpClient();
        var mockApisixLogger = new Mock<ILogger<ApisixClient>>();
        _mockApisixClient = new Mock<ApisixClient>(httpClient, mockApisixLogger.Object);
        _mockLogger = new Mock<ILogger<ConsumerController>>();
        _controller = new ConsumerController(_mockApisixClient.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("valid-user", true)]
    [InlineData("user_01", true)]
    [InlineData("abc", true)]
    [InlineData("user with spaces", false)]
    [InlineData("user;DROP TABLE", false)]
    [InlineData("<script>alert(1)</script>", false)]
    [InlineData("", false)]
    public async Task UpdateConsumer_ValidatesUsername(string username, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            _mockApisixClient.Setup(c => c.UpdateConsumerAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);
        }

        var json = JsonSerializer.Serialize(new { username, quota = new { count = 1000, time_window = 3600, rejected_code = 429, rejected_msg = "exceeded" } });
        var element = JsonDocument.Parse(json).RootElement;

        var result = await _controller.UpdateConsumer(element);

        if (shouldSucceed)
        {
            Assert.IsType<OkResult>(result);
        }
        else
        {
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Theory]
    [InlineData("valid-user", true)]
    [InlineData("user;DROP TABLE", false)]
    public async Task DeleteConsumer_ValidatesUsername(string username, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            _mockApisixClient.Setup(c => c.DeleteConsumerAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        var result = await _controller.DeleteConsumer(username);

        if (shouldSucceed)
        {
            Assert.IsType<NoContentResult>(result);
        }
        else
        {
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task UpdateConsumer_ValidatesMaxLength()
    {
        var longUsername = new string('a', 65); // Exceeds 64 char limit
        var json = JsonSerializer.Serialize(new { username = longUsername });
        var element = JsonDocument.Parse(json).RootElement;

        var result = await _controller.UpdateConsumer(element);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetConsumers_ReturnsOk_WhenSuccessful()
    {
        _mockApisixClient.Setup(c => c.GetConsumersAsync())
            .ReturnsAsync("{\"list\":[]}");

        var result = await _controller.GetConsumers();
        Assert.IsType<OkObjectResult>(result);
    }
}
