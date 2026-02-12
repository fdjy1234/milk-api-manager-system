using MilkApiManager.Services;
using MilkApiManager.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddHttpClient<ApisixClient>();
builder.Services.AddHttpClient<AuditLogService>();
builder.Services.AddHttpClient<PrometheusService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<SecurityAutomationService>();

builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

var app = builder.Build();

// Auto-migrate/ensure created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disable for local testing dev

app.UseAuthorization();

app.MapControllers();

app.Run();