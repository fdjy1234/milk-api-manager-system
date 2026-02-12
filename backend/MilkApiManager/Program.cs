using MilkApiManager.Services;
using MilkApiManager.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext (Upstream Feature)
// Check both connection string paths just in case
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=audit.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// CORS Configuration (User Feature)
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') 
    ?? new[] { "http://localhost:5002", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register Services
builder.Services.AddHttpClient<ApisixClient>();
builder.Services.AddHttpClient<AuditLogService>();
builder.Services.AddHttpClient<PrometheusService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<SecurityAutomationService>();

builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

var app = builder.Build();

// Auto-migrate/ensure created (Upstream Feature)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Global exception handler — prevent stack trace leakage (User Feature)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred.\"}");
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection for non-development environments
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// API Key authentication middleware (User Feature)
var managementApiKey = Environment.GetEnvironmentVariable("MANAGEMENT_API_KEY");
if (!string.IsNullOrEmpty(managementApiKey))
{
    app.Use(async (context, next) =>
    {
        // Skip auth for Swagger endpoints
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next();
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var providedKey) ||
            providedKey != managementApiKey)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Unauthorized. Provide a valid X-API-KEY header.\"}");
            return;
        }

        await next();
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();
