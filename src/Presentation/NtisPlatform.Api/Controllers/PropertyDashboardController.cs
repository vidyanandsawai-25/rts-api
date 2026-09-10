using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyDashboard;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertyDashboardController : ControllerBase
{
    private readonly IPropertyDashboardService _propertyDashboardService;
    private readonly ILogger<PropertyDashboardController> _logger;

    public PropertyDashboardController(IPropertyDashboardService propertyDashboardService, ILogger<PropertyDashboardController> logger)
    {
        _propertyDashboardService = propertyDashboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPropertyDashboardDetailsAsync([FromQuery] PropertyDashboardQueryParameters queryParameters, CancellationToken ct)
    {
        _logger.LogInformation("Request received for PropertyDashboard: ZoneId={ZoneId}, WardId={WardId}, UserId={UserId}",
            queryParameters.ZoneId, queryParameters.WardId, queryParameters.UserId);

        var result = await _propertyDashboardService.GetAllAsync(queryParameters, ct);
        return Ok(result);
    }
}
