using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext
// Check both connection string paths just in case
// Check for Test Mode
var isTestMode = Environment.GetEnvironmentVariable("USE_TEST_MODE") == "true";

if (isTestMode)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("MilkApiManagerTestDb"));
    builder.Services.AddDbContext<AuditContext>(options =>
        options.UseInMemoryDatabase("AuditLogTestDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddDbContext<AuditContext>(options =>
        options.UseNpgsql(connectionString));
}

// Register Services
if (isTestMode)
{
    builder.Services.AddHttpClient<ApisixClient, MockApisixClient>();
}
else
{
    builder.Services.AddHttpClient<ApisixClient>();
}
builder.Services.AddHttpClient<AuditLogService>();
builder.Services.AddHttpClient<PrometheusService>();
builder.Services.AddSingleton<LoadTestService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<SecurityAutomationService>();

builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

// Register NotificationService for AlertMonitoringService
builder.Services.AddHttpClient<NotificationService>();

// Register AlertMonitoringService as Background Service
builder.Services.AddHostedService<AlertMonitoringService>();
// Register Auto-Blocking Security Worker
builder.Services.AddHostedService<AutoBlockWorker>();
// Register Code-First Route Sync
builder.Services.AddHostedService<ApisixRouteSyncService>();

// AuditContext registered above with AppDbContext logic

var app = builder.Build();

// Auto-migrate/ensure created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // For production, consider using db.Database.Migrate() instead of EnsureCreated
    db.Database.EnsureCreated();

    // On startup: if configured to persist blacklist to DB, sync DB entries to APISIX
    // Skip in Test Mode to avoid complexity
    if (!isTestMode)
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var apisix = scope.ServiceProvider.GetRequiredService<ApisixClient>();
        var persist = config.GetValue<bool>("Blacklist:PersistToDatabase");
        
        // 1. Sync Blacklist
        if (persist)
        {
            var entries = db.BlacklistEntries.Select(e => e.IpOrCidr).ToList();
            if (entries.Any())
            {
                try { apisix.UpdateBlacklistAsync(entries).GetAwaiter().GetResult(); }
                catch { /* Log error */ }
            }
        }

        // 2. Register this service in API Catalog
        try 
        {
            var catalog = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!catalog.ApiServices.Any(s => s.Name == "Milk Manager Core"))
            {
                catalog.ApiServices.Add(new ApiServiceMetadata {
                    Name = "Milk Manager Core",
                    Description = "Central API Management Control Plane",
                    BasePath = "/api",
                    OpenApiUrl = "http://localhost:5001/swagger/v1/swagger.json",
                    OwnerTeam = "Platform Team"
                });
                catalog.SaveChanges();
            }
        }
        catch { /* Log error */ }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disable for local testing dev

app.UseAuthorization();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Server", "MilkApiManager"); // Explicitly set or hide in Kestrel
    await next();
});

app.MapControllers();

app.Run();
