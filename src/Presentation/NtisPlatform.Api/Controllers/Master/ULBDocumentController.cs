using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// ULB-wide document metadata (e.g. the certified Tax Zoning List/Map, Ready Reckoner Rate Chart).
/// The actual file upload goes through the existing generic <c>POST /api/documents/upload</c>
/// endpoint with <c>ReferenceTableName = "ULBDocument"</c> and <c>ReferenceTableId</c> set to the
/// Id returned by <see cref="Create"/> — see <c>ULBDocumentBindingHandler</c> for the auto-link step.
/// </summary>
[ApiController]
[Route("api/ulb-documents")]
[Authorize]
public class ULBDocumentController : ControllerBase
{
    private readonly IULBDocumentService _service;
    private readonly IULBDocumentQueryService _queryService;
    private readonly ILogger<ULBDocumentController> _logger;

    public ULBDocumentController(
        IULBDocumentService service,
        IULBDocumentQueryService queryService,
        ILogger<ULBDocumentController> logger)
    {
        _service = service;
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetLatest([FromQuery] string? typeCodes, CancellationToken ct)
    {
        var result = await _queryService.GetLatestAsync(typeCodes, ct);
        return Ok(new ApiResponse<List<ULBDocumentDto>> { Success = true, Items = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateULBDocumentDto createDto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(createDto, ct);
        return Ok(new ApiResponse<int> { Success = true, Message = "Record inserted successfully", Items = id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return Ok(new ApiResponse<object>
        {
            Success = result,
            Message = result ? "Record marked for deletion" : "Record not found"
        });
    }
}
