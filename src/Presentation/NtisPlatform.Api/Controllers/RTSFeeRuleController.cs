using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RTSFeeRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class RTSFeeRuleController : ControllerBase
{
    private readonly IRTSFeeRuleService _service;
    public RTSFeeRuleController(IRTSFeeRuleService service) => _service = service;

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Get([FromQuery] int serviceId, CancellationToken ct)
    {
        if (serviceId <= 0) return BadRequest(new { message = "serviceId is required." });
        return Ok(new ApiResponse<IReadOnlyList<RTSFeeRuleDto>> { Success = true, Items = await _service.GetAsync(serviceId, ct) });
    }

    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound(new { message = "Fee rule was not found." }) : Ok(new ApiResponse<RTSFeeRuleDto> { Success = true, Items = result });
    }

    [AllowAnonymous, HttpGet("ServiceCard")]
    public async Task<IActionResult> GetServiceCards([FromQuery] RTSFeeRuleServiceCardQueryParameters query, CancellationToken ct) =>
        Ok(new ApiResponse<PagedResult<RTSFeeRuleServiceCardDto>> { Success = true, Items = await _service.GetServiceCardsAsync(query, ct) });

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveRTSFeeRuleRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try { return Ok(new ApiResponse<RTSFeeRuleDto> { Success = true, Message = "Fee rule created successfully.", Items = await _service.CreateAsync(request, ct) }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [AllowAnonymous, HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveRTSFeeRuleRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try { return Ok(new ApiResponse<RTSFeeRuleDto> { Success = true, Message = "Fee rule updated successfully.", Items = await _service.UpdateAsync(id, request, ct) }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [AllowAnonymous, HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return Ok(new ApiResponse<object> { Success = true, Message = "Fee rule deleted successfully." }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
