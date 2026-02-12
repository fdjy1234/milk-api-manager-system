using MilkApiManager.Services;
using MilkApiManager.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=audit.db"));

// Register Services
builder.Services.AddHttpClient<ApisixClient>();
builder.Services.AddHttpClient<AuditLogService>();
builder.Services.AddHttpClient<PrometheusService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<SecurityAutomationService>();

builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

var app = builder.Build();

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