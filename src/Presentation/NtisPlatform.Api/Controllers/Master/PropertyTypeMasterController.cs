using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]

public class PropertyTypeMasterController : ControllerBase
{
    private readonly IPropertyTypeMasterService _service;
    private readonly ILogger<PropertyTypeMasterController> _logger;

    public PropertyTypeMasterController(IPropertyTypeMasterService service, ILogger<PropertyTypeMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyTypeMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyTypeMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyTypeMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public async Task<IActionResult> Purge(int id, CancellationToken ct)
    {
        try
        {
            var result = await _service.ForceDeleteAsync(id, ct);
            return result
                ? Ok(new ApiResponse<object> { Success = true, Message = "Record permanently deleted successfully." })
                : Ok(new ApiResponse<object> { Success = false, Message = "Record not found." });
        }
        catch (Exception ex) when (ex is not Application.Exceptions.ValidationException)
        {
            _logger.LogError(ex, "Purge operation failed for id: {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the record"
            });
        }
    }
}
