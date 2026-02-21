using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlacklistController : ControllerBase
    {
        private readonly ApisixClient _apisixClient;
        private readonly ILogger<BlacklistController> _logger;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly AuditLogService _auditLog;

        public BlacklistController(ApisixClient apisixClient, ILogger<BlacklistController> logger, AppDbContext db, IConfiguration config, AuditLogService auditLog)
        {
            _apisixClient = apisixClient;
            _logger = logger;
            _db = db;
            _config = config;
            _auditLog = auditLog;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlacklist()
        {
            try
            {
                var persist = _config.GetValue<bool>("Blacklist:PersistToDatabase");
                if (persist)
                {
                    var entries = await _db.BlacklistEntries.OrderByDescending(e => e.AddedAt).ToListAsync();
                    return Ok(entries);
                }
                else
                {
                    var blacklist = await _apisixClient.GetBlacklistAsync();
                    return Ok(blacklist.Select(ip => new BlacklistEntry { IpOrCidr = ip }).ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blacklist");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBlacklist([FromBody] BlacklistUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Ip))
            {
                return BadRequest("IP is required");
            }

            try
            {
                var blacklist = await _apisixClient.GetBlacklistAsync();
                var blacklistSet = new HashSet<string>(blacklist);

                if (request.Action == "add")
                {
                    blacklistSet.Add(request.Ip);
                    // persist to DB if enabled
                    if (_config.GetValue<bool>("Blacklist:PersistToDatabase"))
                    {
                        var exists = await _db.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == request.Ip);
                        if (exists == null)
                        {
                            var entry = new BlacklistEntry
                            {
                                IpOrCidr = request.Ip,
                                Reason = request.Reason,
                                AddedBy = request.AddedBy,
                                ExpiresAt = request.ExpiresAt,
                                AddedAt = DateTime.UtcNow
                            };
                            _db.BlacklistEntries.Add(entry);
                            await _db.SaveChangesAsync();

                            // Audit log for blacklist add
                            try
                            {
                                await _auditLog.LogAsync(new Models.AuditLogEntry
                                {
                                    Action = "Blacklist.Add",
                                    Resource = "Blacklist",
                                    User = request.AddedBy ?? "Unknown",
                                    Details = new { Ip = request.Ip, Reason = request.Reason, ExpiresAt = request.ExpiresAt }
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write audit log for blacklist add {Ip}", request.Ip);
                            }
                        }
                    }
                }
                else if (request.Action == "remove")
                {
                    blacklistSet.Remove(request.Ip);
                    if (_config.GetValue<bool>("Blacklist:PersistToDatabase"))
                    {
                        var exists = await _db.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == request.Ip);
                        if (exists != null)
                        {
                            // capture details before removal
                            var details = new { Ip = exists.IpOrCidr, Reason = exists.Reason, ExpiresAt = exists.ExpiresAt };

                            _db.BlacklistEntries.Remove(exists);
                            await _db.SaveChangesAsync();

                            // Audit log for blacklist remove
                            try
                            {
                                await _auditLog.LogAsync(new Models.AuditLogEntry
                                {
                                    Action = "Blacklist.Remove",
                                    Resource = "Blacklist",
                                    User = request.AddedBy ?? "Unknown",
                                    Details = details
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write audit log for blacklist remove {Ip}", request.Ip);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid action. Use 'add' or 'remove'.");
                }

                await _apisixClient.UpdateBlacklistAsync(blacklistSet.ToList());
                return Ok(new { message = $"IP {request.Ip} {request.Action}ed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blacklist for IP {Ip}", request.Ip);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class BlacklistUpdateRequest
    {
        public string Ip { get; set; }
        public string Action { get; set; } = "add"; // add | remove
        public string? Reason { get; set; }
        public string? AddedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}