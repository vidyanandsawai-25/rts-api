using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataEntryController : ControllerBase
{
    private readonly IDataEntryService _service;
    private readonly ILogger<DataEntryController> _logger;

    public DataEntryController(IDataEntryService service, ILogger<DataEntryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyDetailsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);



    //[HttpGet("GetByPropertyId/{propertyId}")]
    //public async Task<IActionResult> GetByPropertyId(int propertyId, CancellationToken ct)
    //{
    //    var result = await _service.GetByPropertyIdAsync(propertyId, ct);
    //    return result is null ? NotFound() : Ok(result);
    //}

    [HttpPost]
    public Task<IActionResult> Create( [FromBody] CreatePropertyDetailsDto createDto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(BadRequest(ModelState));
        }

        return this.ExecuteCreate(_service, createDto, _logger, ct);
    }

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);
 
    //[HttpPut("UpdateProperty/{propertyId}")]
    //public async Task<IActionResult> UpdateProperty(int propertyId, [FromBody] UpdatePropertyMastDto updateDto, CancellationToken ct)
    //{
    //    var result = await _service.UpdatePropertyAsync(propertyId, updateDto, ct);
    //    return result is null ? NotFound() : Ok(result);
    //}

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
  
}
