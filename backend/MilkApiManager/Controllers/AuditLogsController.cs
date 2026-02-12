using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogService _auditLogService;

    public AuditLogsController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogEntry>>> Get([FromQuery] int limit = 100)
    {
        var logs = await _auditLogService.GetLogsAsync(limit);
        return Ok(logs);
    }
}
