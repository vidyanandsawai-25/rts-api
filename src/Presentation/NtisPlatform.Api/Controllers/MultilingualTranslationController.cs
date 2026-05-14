using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.MultilingualDetail;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MultilingualTranslationController : ControllerBase
{
    private readonly IMultilingualTranslation _service;
    private readonly ILogger<MultilingualTranslationController> _logger;

    public MultilingualTranslationController(IMultilingualTranslation service, ILogger<MultilingualTranslationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all multilingual translations (paged).
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] MultilingualTranslationQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Bulk Update of translations.
    /// </summary>
    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateMultilingualTranslationDtos>[] items, CancellationToken ct)
    => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    /// <summary>
    /// Gets all distinct resource names.
    /// </summary>
    [HttpGet("GetResources")]
    public async Task<IActionResult> GetResources(CancellationToken ct)
    {
        var result = await _service.GetResourcesAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns whether the auto-translation feature is enabled on the server.
    /// </summary>
    [HttpGet("AutoTranslationConfig")]
    public IActionResult GetAutoTranslationConfig()
    {
        return Ok(new { isEnabled = _service.IsAutoTranslationEnabled() });
    }

}

