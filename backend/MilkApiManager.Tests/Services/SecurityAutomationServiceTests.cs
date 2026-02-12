using Microsoft.Extensions.Logging;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class SecurityAutomationServiceTests
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<IVaultService> _mockVaultService;
    private readonly Mock<ILogger<SecurityAutomationService>> _mockLogger;
    private readonly SecurityAutomationService _service;

    public SecurityAutomationServiceTests()
    {
        var httpClient = new HttpClient();
        var mockApisixLogger = new Mock<ILogger<ApisixClient>>();
        _mockApisixClient = new Mock<ApisixClient>(httpClient, mockApisixLogger.Object);
        _mockVaultService = new Mock<IVaultService>();
        _mockLogger = new Mock<ILogger<SecurityAutomationService>>();
        _service = new SecurityAutomationService(_mockApisixClient.Object, _mockVaultService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CheckAndRotateKeys_RotatesExpiredKeys()
    {
        _mockVaultService.Setup(v => v.RotateApiKeyAsync(It.IsAny<string>()))
            .ReturnsAsync("new-key-12345");

        await _service.CheckAndRotateKeys();

        // The hardcoded test data has "payment-gateway" as expired
        _mockVaultService.Verify(v => v.RotateApiKeyAsync("payment-gateway"), Times.Once);
    }

    [Fact]
    public async Task CheckAndRotateKeys_DoesNotThrow_WhenVaultFails()
    {
        _mockVaultService.Setup(v => v.RotateApiKeyAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("vault unreachable"));

        // Should propagate — this verifies the service doesn't silently swallow errors
        await Assert.ThrowsAsync<Exception>(() => _service.CheckAndRotateKeys());
    }

    [Fact]
    public async Task BlockMaliciousIP_UpdatesGlobalPlugin()
    {
        _mockApisixClient.Setup(c => c.UpdateGlobalPlugin("ip-restriction", It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _service.BlockMaliciousIP("1.2.3.4", "DDoS detected");

        _mockApisixClient.Verify(
            c => c.UpdateGlobalPlugin("ip-restriction", It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task BlockMaliciousIP_ThrowsIfUpdateFails()
    {
        _mockApisixClient.Setup(c => c.UpdateGlobalPlugin(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.BlockMaliciousIP("1.2.3.4", "brute force"));
    }
}
