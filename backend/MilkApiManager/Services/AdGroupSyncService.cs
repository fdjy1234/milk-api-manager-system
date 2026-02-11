using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MilkApiManager.Models.Apisix;

namespace MilkApiManager.Services
{
    public class AdGroupSyncService : IHostedService, IDisposable
    {
        private readonly ILogger<AdGroupSyncService> _logger;
        private readonly ApisixClient _apisixClient;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;
        private string _syncStatus = "Idle";
        private DateTime? _lastSyncTime;

        public AdGroupSyncService(ILogger<AdGroupSyncService> logger, ApisixClient apisixClient, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _apisixClient = apisixClient;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AD Group Sync and Security Automation Service is starting.");
            // Sync and check security every 30 minutes
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            await DoSync();
            await DoSecurityCheck();
        }

        private async Task DoSecurityCheck()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var securityService = scope.ServiceProvider.GetRequiredService<SecurityAutomationService>();
                    await securityService.CheckAndRotateKeys();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Security Lifecycle check.");
            }
        }

        private async Task DoSync()
        {
            _syncStatus = "Syncing";
            try
            {
                _logger.LogInformation("Starting AD Group Sync...");
                
                // Mock AD Group fetching
                var adGroups = await FetchAdGroupsMock();
                
                foreach (var group in adGroups)
                {
                    await SyncGroupToApisix(group);
                    
                    foreach (var member in group.Members)
                    {
                        await SyncUserToApisix(member, group.Name.ToLower());
                    }
                }

                _lastSyncTime = DateTime.UtcNow;
                _syncStatus = "Success";
                _logger.LogInformation("AD Group Sync completed successfully.");
            }
            catch (Exception ex)
            {
                _syncStatus = "Failed";
                _logger.LogError(ex, "Error during AD Group Sync.");
            }
        }

        private async Task<List<AdGroup>> FetchAdGroupsMock()
        {
            // In a real implementation, this would use System.DirectoryServices.AccountManagement
            await Task.Delay(500); // Simulate network delay
            return new List<AdGroup>
            {
                new AdGroup { Name = "Developers", Members = new List<string> { "alice", "bob" } },
                new AdGroup { Name = "Managers", Members = new List<string> { "charlie" } },
                new AdGroup { Name = "Testers", Members = new List<string> { "david", "eve" } }
            };
        }

        private async Task SyncGroupToApisix(AdGroup group)
        {
            var groupId = group.Name.ToLower();
            _logger.LogInformation($"Syncing group {group.Name} to APISIX...");
            
            var groupConfig = new ConsumerGroup
            {
                Id = groupId,
                Plugins = new Dictionary<string, object>()
            };

            await _apisixClient.CreateConsumerGroupAsync(groupId, groupConfig);
        }

        private async Task SyncUserToApisix(string username, string groupId)
        {
            _logger.LogInformation($"Syncing user {username} to group {groupId} in APISIX...");
            
            var consumer = new Consumer
            {
                Username = username,
                GroupId = groupId,
                Plugins = new Dictionary<string, object>()
            };

            await _apisixClient.UpdateConsumerAsync(username, consumer);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AD Group Sync Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public string GetStatus() => _syncStatus;
        public DateTime? GetLastSyncTime() => _lastSyncTime;
    }

    public class AdGroup
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new();
    }
}
