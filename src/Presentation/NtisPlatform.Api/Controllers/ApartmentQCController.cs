using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services.TaxEngine;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApartmentQCController : ControllerBase
{
    private readonly IApartmentQCService _service;
    private readonly IRateableValueService _rateableValueService;
    private readonly ICapitalValueService _capitalValueService;
    private readonly ILogger<ApartmentQCController> _logger;

    private static readonly string[] AllowedFilterFields =
        Enum.GetNames<ApartmentQCFilterColumn>();

    public ApartmentQCController(IApartmentQCService service, IRateableValueService rateableValueService, ICapitalValueService capitalValueService, ILogger<ApartmentQCController> logger)
    {
        _service = service;
        _logger  = logger;
        _rateableValueService = rateableValueService;
        _capitalValueService = capitalValueService;
    }

    /// <summary>
    /// Returns a paginated list of apartment QC records, one aggregated row per property.
    /// </summary>
    /// <response code="200">Filtered apartment QC records (empty list when no matches).</response>
    /// <response code="400">Validation error.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertyApartmentTaxDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] ApartmentQCQueryParameters query, CancellationToken ct)
    {
        var result = await _service.GetPagedAsync(query, ct);
        return Ok(new ApiResponse<PagedResult<PropertyApartmentTaxDto>>
        {
            Success = true,
            Message = result.TotalCount > 0 ? "Record found successfully" : "No records found",
            Items   = result
        });
    }

    /// <summary>
    /// Streams the same rows that <see cref="GetAll"/> would return for the given filter
    /// as a downloadable .xlsx workbook. Pagination is ignored — the caller gets every
    /// matching row up to the configured export cap.
    /// Use <c>section=Rateable</c> for RV columns only, <c>section=Capital</c> for CV columns only,
    /// or omit (defaults to Dual — both sections).
    /// </summary>
    /// <response code="200">Workbook stream (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet).</response>
    /// <response code="400">Matching row count exceeds the configured export cap — narrow the filter.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpGet("export-excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] ApartmentQCQueryParameters query,
        [FromQuery] string? section,
        CancellationToken ct)
    {
        var resultType = ApartmentQCResultType.Dual;
        if (!string.IsNullOrWhiteSpace(section) && !Enum.TryParse(section, ignoreCase: true, out resultType))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"Invalid 'section' value '{section}'. Allowed: Rateable, Capital, Dual."
            });
        }

        var bytes    = await _service.ExportToExcelAsync(query, resultType, ct);
        var fileName = $"ApartmentQC_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Returns one DTO per PropertyDetails row for the given property (expanded view).
    /// </summary>
    /// <param name="id">PropertyMast Id.</param>
    /// <param name="type">Tax-calculation slice: Rateable, Capital, or Dual (default). Case-insensitive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">One row per PropertyDetails (empty list when property has no details).</response>
    /// <response code="400">Invalid type value.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertyApartmentTaxDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByProperty(int id, [FromQuery] string? type, CancellationToken ct)
    {
        var resultType = ApartmentQCResultType.Dual;
        if (!string.IsNullOrWhiteSpace(type) && !Enum.TryParse(type, ignoreCase: true, out resultType))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"Invalid 'type' value '{type}'. Allowed: Rateable, Capital, Dual."
            });
        }

        var result = await _service.GetByPropertyDetailAsync(id, resultType, ct);
        return Ok(new ApiResponse<PagedResult<PropertyApartmentTaxDto>>
        {
            Success = true,
            Message = result.TotalCount > 0 ? "Record found successfully" : "No records found",
            Items   = result
        });
    }

    /// <summary>
    /// Returns distinct values for Wing, ApartmentType, FlatOrShopNo, and PropertyType
    /// across the full scope. <b>WardId and PropertyNo are required.</b>
    /// Column-specific filters are intentionally ignored so every available option is shown.
    /// Use this to populate filter dropdowns before the user applies a column filter.
    /// </summary>
    /// <response code="200">Distinct filter options.</response>
    /// <response code="400">WardId or PropertyNo is missing.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("filter-options")]
    [ProducesResponseType(typeof(ApiResponse<ApartmentQCFilterOptionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilterOptions(
        [FromQuery] ApartmentQCQueryParameters query,
        [FromQuery] string? field,
        CancellationToken ct)
    {
        if (!query.WardId.HasValue || string.IsNullOrWhiteSpace(query.PropertyNo))
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Both 'WardId' and 'PropertyNo' are required for filter options."
            });

        if (field != null && !AllowedFilterFields.Contains(field.Trim(), StringComparer.OrdinalIgnoreCase))
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"Invalid field '{field}'. Allowed: {string.Join(", ", AllowedFilterFields)}."
            });

        var result = await _service.GetFilterOptionsAsync(query, field, ct);
        return Ok(new ApiResponse<ApartmentQCFilterOptionsDto>
        {
            Success = true,
            Message = "Filter options retrieved successfully.",
            Items   = result
        });
    }

    /// <summary>
    /// Looks up a PropertyMastOld record by its OldPropertyNo string and returns
    /// the associated old-data fields for UI auto-fill when the user changes OldPropertyNo.
    /// </summary>
    /// <param name="oldPropertyNo">The OldPropertyNo value to look up (query string).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Old-property data found and returned.</response>
    /// <response code="400">The 'oldPropertyNo' parameter is empty.</response>
    /// <response code="404">No PropertyMastOld record found for the given OldPropertyNo.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("old-property")]
    [ProducesResponseType(typeof(ApiResponse<OldPropertyLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOldPropertyData([FromQuery] string? oldPropertyNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(oldPropertyNo))
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Query parameter 'no' (OldPropertyNo) is required."
            });

        var result = await _service.GetOldPropertyDataAsync(oldPropertyNo, ct);

        if (result is null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No old-property record found for OldPropertyNo '{oldPropertyNo}'."
            });

        return Ok(new ApiResponse<OldPropertyLookupDto>
        {
            Success = true,
            Message = "Old property data retrieved successfully.",
            Items   = result
        });
    }

    /// <summary>
    /// Atomically patches one or more PropertyDetails rows for a property.
    /// All rows are validated before any write — if any row fails, NO rows are written.
    /// </summary>
    /// <param name="propertyId">PropertyMast Id (route).</param>
    /// <param name="dtos">Per-row patches. Each entry must include a positive DetailId and at least one field to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">All rows updated successfully.</response>
    /// <response code="400">Validation failed — body lists every offending row. No changes written.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No active property found for the given propertyId.</response>
    /// <response code="413">Payload exceeded the per-request size limit.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPatch("{propertyId:int}")]
    [RequestSizeLimit(ApartmentQCOptions.BulkUpdateRequestSizeLimit)]
    [ProducesResponseType(typeof(ApiResponse<ApartmentQCBulkUpdateResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ApartmentQCBulkUpdateResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDetail(
        int propertyId,
        [FromBody] List<UpdateApartmentQCDetailsDto> dtos,
        CancellationToken ct)
    {
        if (dtos is null || dtos.Count == 0)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Request body is required and must contain at least one row."
            });

        var userId = GetCurrentUserId();
        _logger.LogInformation(
            "ApartmentQC bulk update requested: PropertyId={PropertyId}, RowCount={RowCount}, ActorUserId={ActorUserId}",
            propertyId, dtos.Count, userId);

        var result = await _service.UpdateDetailAsync(propertyId, dtos, userId, ct);

        if (result is null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active property found for propertyId {propertyId}."
            });

        if (result.Failures.Count > 0)
        {
            _logger.LogWarning(
                "ApartmentQC bulk update rejected: PropertyId={PropertyId}, FailureCount={FailureCount}, ActorUserId={ActorUserId}",
                propertyId, result.Failures.Count, userId);

            return BadRequest(new ApiResponse<ApartmentQCBulkUpdateResultDto>
            {
                Success = false,
                Message = "Bulk update aborted — one or more rows failed validation. No changes were written.",
                Items   = result,
                Errors  = result.Failures.Select(f => $"DetailId {f.DetailId}: {f.Reason}").ToList()
            });
        }

        _logger.LogInformation(
            "ApartmentQC bulk update applied: PropertyId={PropertyId}, UpdatedCount={UpdatedCount}, ActorUserId={ActorUserId}",
            propertyId, result.Updated, userId);

        return Ok(new ApiResponse<ApartmentQCBulkUpdateResultDto>
        {
            Success = true,
            Message = $"{result.Updated} apartment QC detail row(s) updated successfully.",
            Items   = result
        });
    }

    /// <summary>
    /// Partially updates basic property details (owner, occupier, renter, flat/shop info, etc.).
    /// </summary>
    /// <response code="200">Update applied successfully.</response>
    /// <response code="400">Request body is null, no fields provided, or OldPropertyNo does not exist.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No active property found for the given propertyId.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPatch("{propertyId:int}/basic-details")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBasicDetails(
        int propertyId,
        [FromBody] UpdateApartmentQCBasicDetailsDto dto,
        CancellationToken ct)
    {
        if (dto is null)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Request body is required." });

        var outcome = await _service.UpdateBasicDetailsAsync(propertyId, dto, GetCurrentUserId(), ct);

        return outcome switch
        {
            BasicDetailsPatchOutcome.PropertyNotFound => NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active property found for propertyId {propertyId}."
            }),
            BasicDetailsPatchOutcome.OldPropertyNoNotFound => BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"OldPropertyNo '{dto.OldPropertyNo}' does not match any existing record."
            }),
            _ => Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Apartment QC basic details updated successfully."
            })
        };
    }

    /// <summary>
    /// Recomputes CarpetArea, BuiltupArea, and NoOfRooms on a PropertyDetails row
    /// from its current live RoomWiseSubmissionDetails records.
    /// Use this to repair stale aggregates without going through a full DataEntry update.
    /// </summary>
    /// <param name="propertyId">propertyId.</param>
    /// <param name="propertyDetailsId">PropertyDetails Id (not PropertyMast Id).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Aggregates recomputed and saved successfully.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No active PropertyDetails found for the given id.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPost("{propertyId:int}/{propertyDetailsId:int}/sync-rooms")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncRoomAggregates(int propertyId, int propertyDetailsId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Room aggregate sync requested: PropertyDetailsId={PropertyDetailsId}, ActorUserId={ActorUserId}",
                propertyDetailsId, userId);

        var found = await _service.SyncRoomAggregatesAsync(propertyDetailsId, userId, ct);

        if (!found)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active PropertyDetails found for id {propertyDetailsId}."
            });

        await _rateableValueService.CalculateAndSaveAsync(propertyId);
        await _capitalValueService.CreateAsync(new CreateCapitalValueDto{PropertyId = propertyId},ct);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Room aggregate sync completed: PropertyDetailsId={PropertyDetailsId}, ActorUserId={ActorUserId}",
                propertyDetailsId, userId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = $"Room aggregates synced successfully for PropertyDetails {propertyDetailsId}."
        });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }
}
