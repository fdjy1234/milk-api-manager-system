using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class BlacklistControllerTests
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<BlacklistController>> _mockLogger;
    private readonly BlacklistController _controller;

    public BlacklistControllerTests()
    {
        var httpClient = new HttpClient();
        var mockApisixLogger = new Mock<ILogger<ApisixClient>>();
        _mockApisixClient = new Mock<ApisixClient>(httpClient, mockApisixLogger.Object);
        _mockLogger = new Mock<ILogger<BlacklistController>>();
        _controller = new BlacklistController(_mockApisixClient.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.0/24", true)]
    [InlineData("::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("not-an-ip", false)]
    [InlineData("", false)]
    [InlineData("192.168.1.999", false)]
    [InlineData("<script>alert(1)</script>", false)]
    [InlineData("192.168.1.1; DROP TABLE users", false)]
    public async Task UpdateBlacklist_ValidatesIpFormat(string ip, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string>());
            _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        }

        var request = new BlacklistUpdateRequest { Ip = ip, Action = "add" };
        var result = await _controller.UpdateBlacklist(request);

        if (shouldSucceed)
        {
            Assert.IsType<OkObjectResult>(result);
        }
        else
        {
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }

    [Fact]
    public async Task UpdateBlacklist_ReturnsBadRequest_WhenIpIsNull()
    {
        var request = new BlacklistUpdateRequest { Ip = null!, Action = "add" };
        var result = await _controller.UpdateBlacklist(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_ReturnsBadRequest_WhenActionInvalid()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string>());
        var request = new BlacklistUpdateRequest { Ip = "192.168.1.1", Action = "invalid" };
        var result = await _controller.UpdateBlacklist(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBlacklist_ReturnsOk()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "10.0.0.1" });
        var result = await _controller.GetBlacklist();
        Assert.IsType<OkObjectResult>(result);
    }
}
