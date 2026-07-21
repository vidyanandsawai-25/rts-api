using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RTSWorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RTSWorkflowController> _logger;

    public RTSWorkflowController(ApplicationDbContext context, ILogger<RTSWorkflowController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("stages/{serviceId}")]
    public async Task<IActionResult> GetWorkflowStages(int serviceId, CancellationToken ct)
    {
        try
        {
            var flow = await _context.ApprovalFlowMasters
                .FirstOrDefaultAsync(f => f.ServiceId == serviceId && f.IsActive, ct);

            if (flow == null)
            {
                return NotFound(new { status = false, message = "No active workflow configuration found for this service." });
            }

            var stages = await _context.ApprovalFlowStageMasters
                .Where(s => s.ApprovalFlowId == flow.Id)
                .OrderBy(s => s.StageOrder)
                .ToListAsync(ct);

            return Ok(new { status = true, data = new { flowName = flow.ApprovalFlowName, stages = stages } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow stages for ServiceId {ServiceId}", serviceId);
            return StatusCode(500, new { status = false, message = "Internal server error occurred." });
        }
    }
}
