using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IServiceManagementService _serviceManagementService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceManagementService serviceManagementService,
        ILogger<ServicesController> logger)
    {
        _serviceManagementService = serviceManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available services in the platform
    /// </summary>
    /// <returns>List of all services with their details and statistics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        try
        {
            var services = await _serviceManagementService.GetServicesAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving services");
            return StatusCode(500, "An error occurred while retrieving services");
        }
    }
}
