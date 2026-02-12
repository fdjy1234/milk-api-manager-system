using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;
using MilkApiManager.Data;

namespace MilkApiManager.Services;

public class AuditLogService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogService> _logger;
    private readonly string _logstashEndpoint = "http://logstash:8080/";

    public AuditLogService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        IServiceScopeFactory scopeFactory,
        ILogger<AuditLogService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        // 1. Console Logging for Fluentd/Logstash (Upstream Feature)
        if (entry.Timestamp.Kind != DateTimeKind.Utc)
        {
            entry.Timestamp = entry.Timestamp.ToUniversalTime();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(entry, options);
        Console.WriteLine(json);

        // Also attempt to ship to SIEM if configured (User Feature Integration)
        // We'll call ShipLogsToSIEM here if desired, or keep it separate. 
        // For now, let's keep the logic clean and separate unless we want double shipping.

        // 2. Database Logging (Configurable) (Upstream Feature)
        bool enableDb = _configuration.GetValue<bool>("AuditLog:EnableDatabaseWrite");
        if (enableDb)
        {
            // Serialize Details object to string for DB storage
            if (entry.Details != null && string.IsNullOrEmpty(entry.DetailsJson))
            {
                entry.DetailsJson = JsonSerializer.Serialize(entry.Details);
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                // Use IServiceProvider to get the context to avoid disposal issues if factory-created
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Ensure EF Core doesn't track the ID if it's 0 (insert)
                entry.Id = 0; 
                
                dbContext.AuditLogs.Add(entry);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<List<AuditLogEntry>> GetLogsAsync(int limit = 100)
    {
        bool enableDb = _configuration.GetValue<bool>("AuditLog:EnableDatabaseWrite");
        if (!enableDb)
        {
            return new List<AuditLogEntry>();
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }

    /// <summary>
    /// 對齊稽核要求 Q7: 實作 API 呼叫紀錄全量收容 (User Feature)
    /// </summary>
    public virtual async Task ShipLogsToSIEM(object logPayload)
    {
        try 
        {
            var json = JsonSerializer.Serialize(logPayload);
            await _httpClient.PostAsync(_logstashEndpoint, new StringContent(json));
            _logger.LogInformation("[AUDIT] Log shipped to SIEM at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ship logs to SIEM");
        }
    }

    /// <summary>
    /// 定時產出 24h 稽核報表 (Mock 邏輯) (User Feature)
    /// </summary>
    public virtual string GenerateComplianceReport()
    {
        // Mock implementation from User branch
        return "Compliance Report Generated";
    }
}
