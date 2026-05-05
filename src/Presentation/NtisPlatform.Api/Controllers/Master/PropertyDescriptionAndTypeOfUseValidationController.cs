using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyDescriptionAndTypeOfUseValidationController : ControllerBase
{
    private readonly IPropertyDescriptionAndTypeOfUseValidationService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<PropertyDescriptionAndTypeOfUseValidationController> _logger;

    public PropertyDescriptionAndTypeOfUseValidationController(IPropertyDescriptionAndTypeOfUseValidationService service, IHardDeleteCleanupService cleanupService, ILogger<PropertyDescriptionAndTypeOfUseValidationController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyDescriptionAndTypeOfUseValidationQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyDescriptionAndTypeOfUseValidationDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreatePropertyDescriptionAndTypeOfUseValidationDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyDescriptionAndTypeOfUseValidationDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdatePropertyDescriptionAndTypeOfUseValidationDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);


    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<PropertyDescriptionAndTypeOfUseValidationEntity, int>(_cleanupService, id, _logger, ct);

    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<PropertyDescriptionAndTypeOfUseValidationEntity, int>(_cleanupService, ids, _logger, ct);
}
