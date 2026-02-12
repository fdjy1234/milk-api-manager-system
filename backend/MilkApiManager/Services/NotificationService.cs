using MilkApiManager.Models;
using System.Net.Mail;

namespace MilkApiManager.Services
{
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _mattermostWebhookUrl;

        public NotificationService(ILogger<NotificationService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _mattermostWebhookUrl = Environment.GetEnvironmentVariable("MATTERMOST_WEBHOOK_URL") ?? "http://mattermost:8065/hooks/default";
        }

        public async Task SendNotificationAsync(AlertRule rule, string message)
        {
            foreach (var channel in rule.NotificationChannels)
            {
                if (channel == "Mattermost")
                {
                    await SendToMattermostAsync(message);
                }
                else if (channel == "Email")
                {
                    await SendEmailAsync(message);
                }
            }
        }

        private async Task SendToMattermostAsync(string message)
        {
            try
            {
                var payload = new { text = $"### 🚨 API Anomaly Alert\n{message}" };
                var response = await _httpClient.PostAsJsonAsync(_mattermostWebhookUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Sent notification to Mattermost");
                }
                else
                {
                    _logger.LogError("Failed to send Mattermost notification: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Mattermost notification");
            }
        }

        private async Task SendEmailAsync(string message)
        {
            // Dummy email implementation
            _logger.LogInformation("[EMAIL NOTIFICATION] To: admin@milk.com, Msg: {Message}", message);
            await Task.CompletedTask;
        }
    }
}
