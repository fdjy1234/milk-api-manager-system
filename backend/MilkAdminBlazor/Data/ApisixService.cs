using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MilkAdminBlazor.Data
{
    public class ApiRoute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string RiskLevel { get; set; } // L1, L2, L3
        public string Owner { get; set; }
    }

    public class ApiConsumer
    {
        public string Username { get; set; }
        public string Desc { get; set; }
        public Dictionary<string, object> Plugins { get; set; } = new();
        public List<string> Labels { get; set; } = new(); // Used for Roles/Scopes
    }

    public class ApisixService
    {
        private List<ApiConsumer> _mockConsumers = new List<ApiConsumer>
        {
            new ApiConsumer { Username = "client-app-1", Desc = "External Partner A", Labels = new List<string> { "role:premium", "scope:read", "scope:write" } },
            new ApiConsumer { Username = "mobile-app", Desc = "Internal Mobile App", Labels = new List<string> { "role:internal", "scope:read" } },
            new ApiConsumer { Username = "data-aggregator", Desc = "Third-party Analytics", Labels = new List<string> { "role:guest", "scope:read" } }
        };

        public Task<List<ApiConsumer>> GetConsumersAsync()
        {
            return Task.FromResult(_mockConsumers);
        }

        public Task UpdateConsumerAsync(ApiConsumer consumer)
        {
            var existing = _mockConsumers.Find(c => c.Username == consumer.Username);
            if (existing != null)
            {
                existing.Desc = consumer.Desc;
                existing.Labels = consumer.Labels;
                existing.Plugins = consumer.Plugins;
            }
            else
            {
                _mockConsumers.Add(consumer);
            }
            return Task.CompletedTask;
        }

        public Task DeleteConsumerAsync(string username)
        {
            _mockConsumers.RemoveAll(c => c.Username == username);
            return Task.CompletedTask;
        }

        public Task<List<ApiRoute>> GetRoutesAsync()
        {
            // Mock Data for now
            return Task.FromResult(new List<ApiRoute>
            {
                new ApiRoute { Id = "1", Name = "User Profile", Uri = "/api/user/*", RiskLevel = "L3", Owner = "Customer Team" },
                new ApiRoute { Id = "2", Name = "Product List", Uri = "/api/products", RiskLevel = "L1", Owner = "Sales Team" },
                new ApiRoute { Id = "3", Name = "Payment Gateway", Uri = "/api/payment", RiskLevel = "L3", Owner = "Finance Team" },
                new ApiRoute { Id = "4", Name = "Branch Locations", Uri = "/api/locations", RiskLevel = "L1", Owner = "Ops Team" }
            });
        }
    }
}
