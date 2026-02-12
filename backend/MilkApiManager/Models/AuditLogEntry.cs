using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace MilkApiManager.Models;

public class AuditLogEntry
{
    [Key]
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = "System";
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Read
    public string Resource { get; set; } = string.Empty; // e.g. "Route", "Consumer"
    public string? DetailsJson { get; set; } // Store as string for DB flexibility
    
    public object? Details { get; set; } // Keep for runtime/API
}
