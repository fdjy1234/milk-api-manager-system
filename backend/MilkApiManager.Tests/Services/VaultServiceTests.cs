using Microsoft.Extensions.Logging;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class VaultServiceTests
{
    private readonly Mock<ILogger<VaultService>> _mockLogger;
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<AuditLogService> _mockAuditLogService;
    private readonly VaultService _service;

    public VaultServiceTests()
    {
        _mockLogger = new Mock<ILogger<VaultService>>();

        var httpClient = new HttpClient();
        var mockApisixLogger = new Mock<ILogger<ApisixClient>>();
        _mockApisixClient = new Mock<ApisixClient>(httpClient, mockApisixLogger.Object);

        var mockAuditLogger = new Mock<ILogger<AuditLogService>>();
        _mockAuditLogService = new Mock<AuditLogService>(httpClient, mockAuditLogger.Object);

        _service = new VaultService(_mockLogger.Object, _mockApisixClient.Object, _mockAuditLogService.Object);
    }

    [Fact]
    public async Task StoreSecretAsync_ReturnsVersionString()
    {
        var result = await _service.StoreSecretAsync("secret/path", "my-secret");
        Assert.Equal("vault-version-1", result);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsMockValue()
    {
        var result = await _service.GetSecretAsync("secret/path");
        Assert.Equal("mock-secret-value", result);
    }

    [Fact]
    public async Task RotateApiKeyAsync_ReturnsNewKey_AndUpdatesConsumer()
    {
        var consumer = new Consumer
        {
            Username = "test-consumer",
            Plugins = new Dictionary<string, object>()
        };

        _mockApisixClient.Setup(c => c.GetConsumerAsync("test-consumer"))
            .ReturnsAsync(consumer);
        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("test-consumer", It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.ShipLogsToSIEM(It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RotateApiKeyAsync("test-consumer");

        Assert.NotNull(result);
        Assert.Equal(32, result.Length); // Guid.NewGuid().ToString("N") = 32 hex chars

        // Verify APISIX consumer was updated
        _mockApisixClient.Verify(c => c.UpdateConsumerAsync("test-consumer", It.IsAny<object>()), Times.Once);

        // Verify audit log was called
        _mockAuditLogService.Verify(a => a.ShipLogsToSIEM(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RotateApiKeyAsync_ThrowsException_WhenConsumerNotFound()
    {
        _mockApisixClient.Setup(c => c.GetConsumerAsync("unknown"))
            .ReturnsAsync((Consumer?)null);

        var ex = await Assert.ThrowsAsync<Exception>(
            () => _service.RotateApiKeyAsync("unknown"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task RotateApiKeyAsync_CreatesPluginsDict_WhenNull()
    {
        var consumer = new Consumer
        {
            Username = "no-plugins",
            Plugins = null  // Should be auto-created
        };

        _mockApisixClient.Setup(c => c.GetConsumerAsync("no-plugins"))
            .ReturnsAsync(consumer);
        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("no-plugins", It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.ShipLogsToSIEM(It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RotateApiKeyAsync("no-plugins");

        Assert.NotNull(result);
        // The consumer's Plugins dict should now contain key-auth
        Assert.NotNull(consumer.Plugins);
        Assert.True(consumer.Plugins.ContainsKey("key-auth"));
    }
}
