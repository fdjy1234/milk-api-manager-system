using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MilkApiManager.Services
{
    public class ApisixClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApisixClient> _logger;
        private readonly string _adminKey;

        public ApisixClient(HttpClient httpClient, ILogger<ApisixClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // 實際應從 Config 讀取
            _httpClient.BaseAddress = new Uri("http://apisix:9080/apisix/admin/");
            _adminKey = "edd1c9f034335f136f87ad84b625c8f1"; // Default Key
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-API-KEY", _adminKey);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return request;
        }

        public async Task CreateRouteAsync(string id, object routeConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"routes/{id}", routeConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully created route {id}");
        }

        public async Task DeleteRouteAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Delete, $"routes/{id}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to delete route {id}: {response.StatusCode}");
            }
        }
        
        public async Task<string> GetRoutesAsync()
        {
             var request = CreateRequest(HttpMethod.Get, "routes");
             var response = await _httpClient.SendAsync(request);
             return await response.Content.ReadAsStringAsync();
        }
    }
}
