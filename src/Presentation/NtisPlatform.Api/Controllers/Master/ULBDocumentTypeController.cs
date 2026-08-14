using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// CRUD for the <c>PTIS.ULBDocumentType</c> master — the document-category catalog
/// (e.g. "Tax Zoning List", "Ready Reckoner Rate Chart") that <c>ULBDocumentController</c>'s
/// <c>typeCodes</c> query filter and <c>ULBDocumentService.CreateAsync</c> validate against.
/// Deliberately separate from <c>ULBDocumentController</c>, which manages the documents themselves.
/// </summary>
[ApiController]
[Route("api/ulb-document-types")]
[Authorize]
public class ULBDocumentTypeController : ControllerBase
{
    private readonly IULBDocumentTypeService _service;
    private readonly ILogger<ULBDocumentTypeController> _logger;

    public ULBDocumentTypeController(IULBDocumentTypeService service, ILogger<ULBDocumentTypeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return Ok(new ApiResponse<List<ULBDocumentTypeDto>> { Success = true, Items = result });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null
            ? NotFound(new ApiResponse<ULBDocumentTypeDto> { Success = false, Message = "Record not found" })
            : Ok(new ApiResponse<ULBDocumentTypeDto> { Success = true, Items = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateULBDocumentTypeDto createDto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(createDto, ct);
        return Ok(new ApiResponse<int> { Success = true, Message = "Record inserted successfully", Items = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateULBDocumentTypeDto updateDto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, updateDto, ct);
        return Ok(new ApiResponse<object>
        {
            Success = result,
            Message = result ? "Record updated successfully" : "Record not found"
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return Ok(new ApiResponse<object>
        {
            Success = result,
            Message = result ? "Record deactivated successfully" : "Record not found"
        });
    }
}
