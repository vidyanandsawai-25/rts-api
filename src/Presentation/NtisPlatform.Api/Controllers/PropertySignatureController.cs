using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertySignature;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for property sign-off operations.
/// Handles Clerk, Tax Inspector, Assistant Commissioner, and Additional Commissioner approvals.
/// All endpoints require authentication. UserId is extracted from the JWT token.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertySignatureController : ControllerBase
{
 
    private readonly IPropertySignatureService _signatureService;
    private readonly ILogger<PropertySignatureController> _logger;

    public PropertySignatureController(
        IPropertySignatureService signatureService,
        ILogger<PropertySignatureController> logger)
    {
        _signatureService = signatureService;
        _logger = logger;
    }
 

    /// <summary>
    /// Returns all active signing authorities in sequential order:
    /// Clerk → Tax Inspector → Assistant Commissioner → Additional Commissioner.
    /// </summary>
    [HttpGet("authorities")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<SignAuthorityDto>>>> GetAuthorities(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetAuthoritiesAsync(cancellationToken);
            return OkItems(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sign authorities");
            throw;
        }
    }

    /// <summary>
    /// Downloads the Excel template for PropertySignature upload.
    /// Required columns: PropertyId. Optional column: Remarks.
    /// </summary>
    [HttpGet("approve/template-excel")]
    public async Task<IActionResult> DownloadApprovalTemplate(CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _signatureService.GetApprovalUploadTemplateAsync(cancellationToken);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "PropertySignatureUploadTemplate.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading approval template");
            throw;
        }
    }
 

    /// <summary>
    /// Returns zone-wise / division-wise sign-off grid data.
    /// Shows Clerk, Tax Inspector, Assistant Commissioner, and Additional Commissioner
    /// signed property counts (structure and unit) and total demands.
    /// </summary>
    [HttpGet("dashboard/sign-grid")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<SignAuthorityGridResponseDto>>>> GetSignAuthorityGrid(
        [FromQuery] int? zoneId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = zoneId.HasValue
                ? new PropertySearchRequestDto { ZoneId = zoneId.Value }
                : null;

            var result = await _signatureService.GetSignAuthorityGridDataAsync(searchRequest, cancellationToken);

            return OkItem(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sign-off grid statistics");
            throw;
        }
    }

    /// <summary>
    /// Returns ward-wise sign-off grid data for a specific zone.
    /// Shows Clerk, Tax Inspector, Assistant Commissioner, and Additional Commissioner
    /// signed property counts (structure and unit) and total demands for each ward in the zone.
    /// </summary>
    [HttpGet("dashboard/ward-wise-summary/zone")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<SignAuthorityWardGridResponseDto>>>> GetSignAuthorityWardGrid(
        [FromQuery] PropertySignatureWardGridQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetSignAuthorityWardGridDataAsync(queryParameters, cancellationToken);

            return OkItem(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ward-wise sign-off grid for ZoneId={ZoneId}", queryParameters.ZoneId);
            throw;
        }
    }

    /// <summary>
    /// Returns building-wise signature data for a specific ward and workflow stage.
    /// </summary>
    [HttpGet("GetBuildingWiseData")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>>>>> GetBuildingWiseData(
        [FromQuery] PropertySignatureBuildingWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetSubGridAsync(queryParameters, cancellationToken);

            return OkItem(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving building-wise data for WardId={WardId}, WorkflowStageId={WorkflowStageId}",
                queryParameters.WardId, queryParameters.WorkflowStageId);
            throw;
        }
    }

    /// <summary>
    /// Returns property-wise signature data including PropertyId and all signature details.
    /// Supports pagination and filters by building/property number.
    /// </summary>
    [HttpGet("GetPropertyWiseData")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>>>>> GetPropertyWiseData(
        [FromQuery] PropertySignaturePropertyWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetPropertyWiseDataAsync(queryParameters, cancellationToken);

            return OkItem(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property-wise data for PropertyNo={PropertyNo}", queryParameters.PropertyNo);
            throw;
        }
    }



    /// <summary>
    /// Returns export-ready rows for properties pending at the selected signing authority.
    /// </summary>
    [HttpGet("pending-export")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<PropertySignaturePendingExportDto>>>> GetPendingExportData(
        [FromQuery] int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetPendingExportDataAsync(signAuthorityId, cancellationToken);

            return OkItems(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending export data for SignAuthorityId={SignAuthorityId}", signAuthorityId);
            throw;
        }
    }

    /// <summary>
    /// Returns pending sign rows for the selected user and signing authority.
    /// </summary>
    [HttpGet("GetPendingSigns")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<PropertySignaturePagedResultDto<PropertySignaturePendingSignDto>>>>> GetPendingSigns(
        [FromQuery] PropertySignaturePendingSignsQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetPendingSignsAsync(queryParameters, cancellationToken);

            return OkItem(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving pending signs for UserId={UserId}", queryParameters.UserId);
            throw;
        }
    }

    /// <summary>
    /// Updates the current pending signature status and creates the next pending signature row when applicable.
    /// </summary>
    [HttpPut("UpdatePropertySign")]
    public async Task<ActionResult<PropertySignatureItemsResponse<IReadOnlyList<PropertySignatureUpdateSignResponseDto>>>> UpdatePropertySign(
        [FromBody] PropertySignatureUpdateSignRequestDto request,CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.UpdateSignAsync(request, cancellationToken);
             return OkItem(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating signature for UserId={UserId}, PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                request.UserId,
                request.PropertyId,
                request.SignAuthorityId);
            throw;
        }
    }

    /// <summary>
    /// Extracts UserId from JWT token claims.
    /// </summary>
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst(ClaimTypes.Name)?.Value;

        return int.TryParse(claim, out var id) ? id : 0;
    }

    private ActionResult<PropertySignatureItemsResponse<IReadOnlyList<T>>> OkItem<T>(T item)
        => OkItems(new[] { item });

    private ActionResult<PropertySignatureItemsResponse<IReadOnlyList<T>>> OkItems<T>(IEnumerable<T> items)
        => Ok(new PropertySignatureItemsResponse<IReadOnlyList<T>>
        {
            Items = items.ToList()
        });
}

public sealed class PropertySignatureItemsResponse<T>
{
    public T? Items { get; set; }
}
