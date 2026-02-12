using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogController : ControllerBase
{
    private readonly AuditContext _context;

    public AuditLogController(AuditContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogEntry>>> GetLogs()
    {
        return await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToListAsync();
    }
}
