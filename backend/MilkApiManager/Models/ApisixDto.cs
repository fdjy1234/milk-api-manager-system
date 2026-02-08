using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MilkApiManager.Models.Apisix
{
    public class Route
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        
        [JsonPropertyName("uris")]
        public List<string>? Uris { get; set; }

        [JsonPropertyName("methods")]
        public List<string>? Methods { get; set; }

        [JsonPropertyName("service_id")]
        public string ServiceId { get; set; }
    }

    public class Service
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("upstream")]
        public Upstream Upstream { get; set; }
    }

    public class Upstream
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "roundrobin";

        [JsonPropertyName("nodes")]
        public Dictionary<string, int> Nodes { get; set; }
    }

    public class Consumer
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object> Plugins { get; set; }
    }
}
