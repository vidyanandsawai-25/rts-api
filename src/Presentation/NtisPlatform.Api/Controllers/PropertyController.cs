using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Api.Filters;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Core Property Aggregate API - Provides property search and lookup functionality.
/// Used across multiple features (ApplyTaxes, BillGeneration, Reports, etc.).
/// </summary>
/// <remarks>
/// Unlike other simple master data controllers (e.g. BankMaster, Ward, Zone) that live under
/// Controllers/Master, the Property aggregate is a core, cross-cutting domain concept used
/// by multiple bounded contexts and workflows. For this reason, it is intentionally exposed
/// as a root-level API at route <c>/api/Property</c> rather than being grouped under the
/// Master controllers folder.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[TypeFilter(typeof(PropertyApiExceptionFilter))]
public partial class PropertyController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IPropertyBasicDetailsService _propertyBasicDetailsService;
    private readonly IPropertyKycService _propertyKycService;
    private readonly IPropertySocietyService _propertySocietyService;
    private readonly IPropertyDiscountService _propertyDiscountService;
    private readonly IPropertyOldDetailsService _propertyOldDetailsService;
    private readonly IPropertySearchService _propertySearchService;
    private readonly ILogger<PropertyController> _logger;
    private readonly IPropertyDiscountDocumentService _discountDocumentService;
    private readonly IWebHostEnvironment _environment;
    private readonly FileValidationHelper _fileValidationHelper;

    /// <summary>
    /// Constructor follows codebase convention: Service dependencies first, then infrastructure.
    /// </summary>
    public PropertyController(
        IPropertyService propertyService,
        IPropertyBasicDetailsService propertyBasicDetailsService,
        IPropertyKycService propertyKycService,
        IPropertySocietyService propertySocietyService,
        IPropertyDiscountService propertyDiscountService,
        IPropertyOldDetailsService propertyOldDetailsService,
        IPropertySearchService propertySearchService,
        ILogger<PropertyController> logger,
        IPropertyDiscountDocumentService discountDocumentService,
        IWebHostEnvironment environment,
        FileValidationHelper fileValidationHelper)
    {
        _propertyService = propertyService;
        _propertyBasicDetailsService = propertyBasicDetailsService;
        _propertyKycService = propertyKycService;
        _propertySocietyService = propertySocietyService;
        _propertyDiscountService = propertyDiscountService;
        _propertyOldDetailsService = propertyOldDetailsService;
        _propertySearchService = propertySearchService;
        _logger = logger;
        _discountDocumentService = discountDocumentService;
        _environment = environment;
        _fileValidationHelper = fileValidationHelper;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyQueryParameters query, CancellationToken ct)
        => this.ExecuteGetAllPaged(_propertyService, query, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_propertyService, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyService, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_propertyService, id, updateDto, _logger, ct);

    [Authorize]
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_propertyService, id, _logger, ct);

    /// <summary>
    /// Deletes multiple property records by their IDs with transactional consistency.
    /// Properties are soft-deleted by setting MarkedForDeletion=true and IsActive=false.
    /// </summary>
    /// <param name="ids">Array of property IDs to delete. Must not be null or empty.</param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>
    /// 200 OK with BulkResult containing success count and any errors,
    /// 400 Bad Request if ids array is null or empty,
    /// 500 Internal Server Error if a critical failure occurs
    /// </returns>
    /// <response code="200">Returns bulk delete result with success/failure details for each property</response>
    /// <response code="400">If the ids array is null, empty, or contains invalid values</response>
    /// <response code="500">If a critical error occurs during the deletion process</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/Property/Bulk
    ///     [1, 2, 3, 4, 5]
    ///     
    /// **Transaction Behavior:**
    /// - All database changes occur within a single database transaction
    /// - If any property passes validation and is deleted, all those successful deletions are committed together
    /// - If a critical error occurs (database error, system failure), ALL changes are rolled back
    /// 
    /// **Partial Success:**
    /// This endpoint supports partial success where individual properties may be skipped (not deleted) due to:
    /// - Property not found (404)
    /// - Property already deleted
    /// - Validation failures
    /// - Business rule violations
    /// 
    /// Successfully deleted properties are committed even if others fail validation.
    /// 
    /// **Related Data:**
    /// All related entities (PropertyDetails, PlotDetails, SocietyDetails, etc.) are also soft-deleted.
    /// </remarks>
    [Authorize]
    [HttpDelete("Bulk")]
    [ProducesResponseType(typeof(BulkResult<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_propertyService, ids, _logger, ct);
        
    /// <summary>
    /// Creates multiple property records from a specified range with transactional consistency.
    /// </summary>
    /// <param name="request">The range create request containing property template and range parameters.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// 200 OK with RangeResult containing success count, failed count, and created properties,
    /// 400 Bad Request if the request is invalid,
    /// 409 Conflict if duplicate properties are detected,
    /// 500 Internal Server Error if a critical failure occurs.
    /// </returns>
    /// <response code="200">Returns RangeResult with successfully created properties and any errors.</response>
    /// <response code="400">If the request is null, template is missing, or range parameters are invalid.</response>
    /// <response code="409">If duplicate property numbers are detected within the specified range.</response>
    /// <response code="500">If a critical error occurs during the creation process.</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/Property/Range
    ///     {
    ///         "rangeFrom": "1",
    ///         "rangeTo": "10",
    ///         "prefix": "PROP-",
    ///         "suffix": "",
    ///         "startSequenceNo": 1,
    ///         "template": {
    ///             "wardId": 1,
    ///             "zoneId": 1,
    ///             // Other CreateNewPropertyDto fields
    ///         }
    ///     }
    ///     
    /// **Transaction Behavior:**
    /// - All database changes occur within a single database transaction.
    /// - If any property fails validation or creation, all changes are rolled back.
    /// - If a critical error occurs (database error, system failure), ALL changes are rolled back.
    /// 
    /// **Range Processing:**
    /// - Properties are created sequentially from RangeFrom to RangeTo using the provided template.
    /// - PropertyNo is generated using the pattern: {Prefix}{RangeValue}{Suffix}
    /// - PropertySeqNo is set to the numeric range value.
    /// 
    /// **Error Handling:**
    /// - If a property already exists, the error is recorded and the entire operation is rolled back.
    /// - All errors are collected and returned in the Errors array of the response.
    /// </remarks>
    [Authorize]
    [HttpPost("Range")]
    [ProducesResponseType(typeof(RangeResult<CreateNewPropertyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFromRange([FromBody] RangeCreateRequest<CreateNewPropertyDto> request, CancellationToken ct)
    {
        var result = await _propertyService.CreatePropertiesFromRangeAsync(request, ct);
        return Ok(result);
    }
}

