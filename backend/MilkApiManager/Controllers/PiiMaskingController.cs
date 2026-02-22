using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using System.Text.Json;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PiiMaskingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ApisixClient _apisixClient;
    private readonly ILogger<PiiMaskingController> _logger;

    public PiiMaskingController(AppDbContext context, ApisixClient apisixClient, ILogger<PiiMaskingController> logger)
    {
        _context = context;
        _apisixClient = apisixClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PiiMaskingRule>>> GetRules()
    {
        return await _context.PiiMaskingRules.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<PiiMaskingRule>> CreateRule(PiiMaskingRule rule)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.PiiMaskingRules.Add(rule);
        await _context.SaveChangesAsync();

        await SyncToApisix(rule.RouteId);
        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRule(int id, PiiMaskingRule rule)
    {
        if (id != rule.Id) return BadRequest();

        rule.UpdatedAt = DateTime.UtcNow;
        _context.Entry(rule).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            await SyncToApisix(rule.RouteId);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RuleExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.PiiMaskingRules.FindAsync(id);
        if (rule == null) return NotFound();

        var routeId = rule.RouteId;
        _context.PiiMaskingRules.Remove(rule);
        await _context.SaveChangesAsync();

        await SyncToApisix(routeId);
        return NoContent();
    }

    private async Task SyncToApisix(string routeId)
    {
        try
        {
            var activeRules = await _context.PiiMaskingRules
                .Where(r => r.RouteId == routeId && r.IsActive)
                .ToListAsync();

            var route = await _apisixClient.GetRouteAsync(routeId);
            if (route == null)
            {
                _logger.LogWarning($"Route {routeId} not found in APISIX. Skipping PII sync.");
                return;
            }

            if (activeRules.Count == 0)
            {
                // Remove the plugin if no active rules
                if (route.Plugins != null)
                {
                    var pluginsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(route.Plugins));
                    if (pluginsDict != null && pluginsDict.Remove("pii-masker"))
                    {
                        route.Plugins = pluginsDict;
                        await _apisixClient.UpdateRouteAsync(routeId, route);
                    }
                }
                return;
            }

            // Construct plugin config
            var piiConfig = new
            {
                rules = activeRules.Select(r => new
                {
                    field_name = r.FieldPath,
                    regex = r.RegexPattern,
                    replace = r.ReplacePattern
                }).ToList()
            };

            // Inject into route plugins
            var currentPlugins = route.Plugins != null 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(route.Plugins)) 
                : new Dictionary<string, object>();
            
            if (currentPlugins != null)
            {
                currentPlugins["pii-masker"] = piiConfig;
                route.Plugins = currentPlugins;
                await _apisixClient.UpdateRouteAsync(routeId, route);
                _logger.LogInformation($"Successfully synced {activeRules.Count} PII rules to route {routeId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing PII rules for route {routeId}");
        }
    }

    private bool RuleExists(int id) => _context.PiiMaskingRules.Any(e => e.Id == id);
}
