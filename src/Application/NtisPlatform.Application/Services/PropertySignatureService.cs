using AutoMapper;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using System.Globalization;
using ClosedXML.Excel;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for property sign-off operations.
/// Implements sequential approval workflow: Clerk → Tax Inspector → Assistant Commissioner → Additional Commissioner.
/// </summary>
public class PropertySignatureService : IPropertySignatureService
{
    #region Constants

    private const string SequenceViolationReason = "Property has not been approved by the previous signing authority.";
    private const string AlreadyApprovedReason = "Property is already approved by this signing authority.";
    private const string ReasonKey = "Reason";
    private const string PendingSignAtKey = "PendingSignAt";
    private const string PendingOfficerNameKey = "PendingOfficerName";

    #endregion

    #region Fields

    private readonly IPropertySignatureRepository _repository;
    private readonly IExcelUploadService _excelUploadService;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertySignatureService> _logger;

    #endregion

    #region Constructor

    public PropertySignatureService(
        IPropertySignatureRepository repository,
        IExcelUploadService excelUploadService,
        IMapper mapper,
        ILogger<PropertySignatureService> logger)
    {
        _repository = repository;
        _excelUploadService = excelUploadService;
        _mapper = mapper;
        _logger = logger;
    }

    #endregion

    #region Public API Methods

    /// <summary>
    /// Gets all active signing authorities in sequential order.
    /// </summary>
    public async Task<List<SignAuthorityDto>> GetAuthoritiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all signing authorities");
            return await _repository.GetAuthoritiesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving signing authorities");
            throw;
        }
    }

    /// <summary>
    /// Gets properties eligible for signing by the specified authority.
    /// Enforces sequential approval rules.
    /// </summary>
    public async Task<List<EligiblePropertyDto>> GetEligiblePropertiesAsync(
        int signAuthorityId,
        int? zoneId,
        int? wardId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving eligible properties for SignAuthorityId={SignAuthorityId}, ZoneId={ZoneId}, WardId={WardId}",
                signAuthorityId, zoneId, wardId);

            return await GetEligiblePropertiesCoreAsync(signAuthorityId, zoneId, wardId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving eligible properties for SignAuthorityId={SignAuthorityId}",
                signAuthorityId);
            throw;
        }
    }

    /// <summary>
    /// Submits property approvals with business rule validation.
    /// Rules: 1) Sequential approval required, 2) No duplicate approvals.
    /// </summary>
    public async Task<SubmitSignatureResponseDto> SubmitApprovalsAsync( int userId, SubmitSignatureRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "User {UserId} submitting {Count} approval(s) for SignAuthorityId={SignAuthorityId}",
                userId, request.PropertyApprovals.Count, request.SignAuthorityId);

            var rejected = new List<RejectedPropertyDto>();
            var toApprove = new List<PropertyApprovalItemDto>();

            var requestedPropertyIds = request.PropertyApprovals.Select(p => p.PropertyId).Distinct().ToList();

            // Business Rule 1: Sequential Check
            var sequenceViolations = await GetSequenceViolationIdsAsync(requestedPropertyIds, request.SignAuthorityId, cancellationToken);

            rejected.AddRange(MapRejectedProperties(sequenceViolations, SequenceViolationReason));

            // Business Rule 2: Duplicate Check
            var eligibleIds = requestedPropertyIds.Except(sequenceViolations).ToList();

            if (eligibleIds.Any())
            {
                var alreadyApproved = await _repository.GetAlreadyApprovedPropertyIdsAsync(
                    eligibleIds, request.SignAuthorityId, cancellationToken);

                rejected.AddRange(MapRejectedProperties(alreadyApproved, AlreadyApprovedReason));

                var finalIds = eligibleIds.Except(alreadyApproved).ToHashSet();
                toApprove = request.PropertyApprovals
                    .Where(p => finalIds.Contains(p.PropertyId))
                    .ToList();
            }

            // Save valid approvals
            int savedCount = 0;
            if (toApprove.Any())
            {
                savedCount = await _repository.SaveApprovalsAsync(
                    userId, request.SignAuthorityId, toApprove, cancellationToken);

                _logger.LogInformation(
                    "Successfully saved {Count} approval(s) for User {UserId}",
                    savedCount, userId);
            }

            return new SubmitSignatureResponseDto
            {
                ApprovedCount = savedCount,
                RejectedProperties = rejected,
                Message = BuildApprovalMessage(savedCount, rejected.Count)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error submitting approvals for User {UserId}, SignAuthorityId={SignAuthorityId}",
                userId, request.SignAuthorityId);
            throw;
        }
    }

    /// <summary>
    /// Imports property approvals from Excel file.
    /// </summary>

    /// <summary>
    /// Imports property approvals from Excel file.
    /// </summary>
    public async Task<PropertySignatureExcelUploadResultDto> UploadApprovalsFromExcelAsync(
        int userId,
        int signAuthorityId,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "User {UserId} uploading Excel approvals for SignAuthorityId={SignAuthorityId}",
                userId, signAuthorityId);

            var (headers, rows) = _excelUploadService.Read(fileStream);
            var missingHeaders = _excelUploadService.GetMissingRequiredHeaders(
                headers,
                new[] { "PropertyId" });

            if (missingHeaders.Count > 0)
                throw new ArgumentException(
                    $"Missing required column(s): {string.Join(", ", missingHeaders)}.");

            var (approvals, parseRejected) = ParseExcelRows(rows);

            if (approvals.Count == 0)
            {
                _logger.LogWarning("No valid rows found in uploaded Excel file");
                return new PropertySignatureExcelUploadResultDto
                {
                    TotalRows = rows.Count,
                    ValidRows = 0,
                    ApprovedCount = 0,
                    RejectedProperties = parseRejected,
                    Message = "No valid rows were found in the uploaded file."
                };
            }

            var submitResult = await SubmitApprovalsAsync(
                userId,
                new SubmitSignatureRequestDto
                {
                    SignAuthorityId = signAuthorityId,
                    PropertyApprovals = approvals
                },
                cancellationToken);

            var mergedRejected = parseRejected.Concat(submitResult.RejectedProperties).ToList();

            _logger.LogInformation(
                "Excel upload complete: {Approved} approved, {Rejected} rejected",
                submitResult.ApprovedCount, mergedRejected.Count);

            return new PropertySignatureExcelUploadResultDto
            {
                TotalRows = rows.Count,
                ValidRows = approvals.Count,
                ApprovedCount = submitResult.ApprovedCount,
                RejectedProperties = mergedRejected,
                Message = submitResult.ApprovedCount > 0
                    ? $"{submitResult.ApprovedCount} property approval(s) imported successfully."
                    : "No approvals were imported."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error uploading Excel approvals for User {UserId}",
                userId);
            throw;
        }
    }

    /// <summary>
    /// Generates Excel template for uploading property approvals.
    /// </summary>
    public Task<byte[]> GetApprovalUploadTemplateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating approval upload template");
            cancellationToken.ThrowIfCancellationRequested();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("PropertySignatureUpload");

            sheet.Cell(1, 1).Value = "PropertyId";
            sheet.Cell(1, 2).Value = "Remarks";

            // Example row
            sheet.Cell(2, 1).Value = 1001;
            sheet.Cell(2, 2).Value = "Optional remark";

            var headerRange = sheet.Range(1, 1, 1, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            sheet.Column(1).Width = 18;
            sheet.Column(2).Width = 45;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating approval template");
            throw;
        }
    }

    /// <summary>
    /// Gets approvals submitted by the current user.
    /// </summary>
    public async Task<List<SignatureApprovalDto>> GetMyApprovalsAsync(
        int userId,
        int signAuthorityId,
        int? zoneId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving approvals for User {UserId}, SignAuthorityId={SignAuthorityId}",
                userId, signAuthorityId);

            return await _repository.GetMyApprovalsAsync(userId, signAuthorityId, zoneId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving approvals for User {UserId}",
                userId);
            throw;
        }
    }

    /// <summary>
    /// Gets complete sign-off chain status for a property.
    /// </summary>
    public async Task<PropertySignatureStatusDto?> GetPropertySignatureStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving signature status for PropertyId={PropertyId}", propertyId);
            return await _repository.GetPropertySignatureStatusAsync(propertyId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving signature status for PropertyId={PropertyId}",
                propertyId);
            throw;
        }
    }

    /// <summary>
    /// Revokes (soft-deletes) an approval.
    /// </summary>
    public async Task<bool> RevokeApprovalAsync(
        int propertyId,
        int signAuthorityId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Revoking approval for PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}, User={UserId}",
                propertyId, signAuthorityId, updatedBy);

            var revoked = await _repository.RevokeApprovalAsync(
                propertyId, signAuthorityId, updatedBy, cancellationToken);

            if (revoked)
                _logger.LogInformation("Successfully revoked approval for PropertyId={PropertyId}", propertyId);
            else
                _logger.LogWarning("No active approval found to revoke for PropertyId={PropertyId}", propertyId);

            return revoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error revoking approval for PropertyId={PropertyId}",
                propertyId);
            throw;
        }
    }

    /// <summary>
    /// Gets sign-off grid data (zone-wise or division-wise).
    /// </summary>
    public async Task<SignAuthorityGridResponseDto> GetSignAuthorityGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving sign-off grid data");
            return await _repository.GetSignAuthorityGridDataAsync(searchRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sign-off grid data");
            throw;
        }
    }

    /// <summary>
    /// Gets ward-wise sign-off grid data for a zone.
    /// </summary>
    public async Task<SignAuthorityGridResponseDto> GetSignAuthorityWardGridDataAsync(
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving ward-wise grid data for ZoneId={ZoneId}", zoneId);
            return await _repository.GetSignAuthorityWardGridDataAsync(zoneId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving ward-wise grid data for ZoneId={ZoneId}",
                zoneId);
            throw;
        }
    }

    /// <summary>
    /// Gets building-wise sub-grid data.
    /// </summary>
    public async Task<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>> GetSubGridAsync(
        PropertySignatureBuildingWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving sub-grid data for WardId={WardId}, WorkflowStageId={WorkflowStageId}, NoticeNo={NoticeNo}",
                queryParameters.WardId, queryParameters.WorkflowStageId, queryParameters.NoticeNo);

            return await _repository.GetBuildingWiseDataAsync(
                queryParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving sub-grid data for WardId={WardId}",
                queryParameters.WardId);
            throw;
        }
    }

    /// <summary>
    /// Gets property-wise signature data.
    /// </summary>
    public async Task<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>> GetPropertyWiseDataAsync(
        PropertySignaturePropertyWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving property-wise data for PropertyNo={PropertyNo}, SearchType={SearchType}",
                queryParameters.PropertyNo, queryParameters.SearchType);

            return await _repository.GetPropertyWiseDataAsync(
                queryParameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving property-wise data for PropertyNo={PropertyNo}",
                queryParameters.PropertyNo);
            throw;
        }
    }

    /// <summary>
    /// Gets pending properties for export.
    /// </summary>
    public async Task<List<PropertySignaturePendingExportDto>> GetPendingExportDataAsync(
        int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving pending export data for SignAuthorityId={SignAuthorityId}",
                signAuthorityId);

            var authorities = await _repository.GetPendingExportAuthoritiesAsync(cancellationToken);
            var currentAuthority = authorities.FirstOrDefault(a => a.SignAuthorityId == signAuthorityId);

            if (currentAuthority == null)
            {
                _logger.LogWarning("SignAuthorityId={SignAuthorityId} not found", signAuthorityId);
                return new List<PropertySignaturePendingExportDto>();
            }

            var lowerAuthorityIds = authorities
                .Where(a => a.SequenceOrder < currentAuthority.SequenceOrder)
                .Select(a => a.SignAuthorityId)
                .ToHashSet();

            var currentAndUpperAuthorityIds = authorities
                .Where(a => a.SequenceOrder >= currentAuthority.SequenceOrder)
                .Select(a => a.SignAuthorityId)
                .ToHashSet();

            var sourceRows = await _repository.GetPendingExportSourceDataAsync(cancellationToken);

            return sourceRows
                .Where(row =>
                {
                    var signedAuthorityIds = row.SignedAuthorityIds.ToHashSet();
                    var hasAllLowerSignatures = lowerAuthorityIds.All(signedAuthorityIds.Contains);
                    var hasCurrentOrUpperSignature = currentAndUpperAuthorityIds.Any(signedAuthorityIds.Contains);

                    return hasAllLowerSignatures && !hasCurrentOrUpperSignature;
                })
                .Select(row => MapPendingExportRow(row, currentAuthority))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving pending export data for SignAuthorityId={SignAuthorityId}",
                signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets properties eligible for signing with sequential rule enforcement.
    /// </summary>
    private async Task<List<EligiblePropertyDto>> GetEligiblePropertiesCoreAsync(
        int signAuthorityId,
        int? zoneId,
        int? wardId,
        CancellationToken cancellationToken)
    {
        var authorities = await _repository.GetAuthoritiesAsync(cancellationToken);
        var currentAuthority = authorities.FirstOrDefault(a => a.Id == signAuthorityId);

        if (currentAuthority == null)
        {
            _logger.LogWarning("SignAuthorityId={SignAuthorityId} not found", signAuthorityId);
            return new List<EligiblePropertyDto>();
        }

        var candidates = await _repository.GetEligiblePropertiesAsync(
            signAuthorityId, zoneId, wardId, cancellationToken);
        var candidateIds = candidates.Select(p => p.PropertyId).Distinct().ToList();

        if (!candidateIds.Any())
            return candidates;

        var eligibleIds = candidateIds.ToHashSet();

        // Enforce sequential approval rule
        if (currentAuthority.SequenceOrder > 1)
        {
            var previousAuthority = authorities
                .Where(a => a.SequenceOrder < currentAuthority.SequenceOrder)
                .OrderByDescending(a => a.SequenceOrder)
                .FirstOrDefault();

            if (previousAuthority == null)
                return new List<EligiblePropertyDto>();

            var previousSignedIds = await _repository.GetSignedPropertyIdsAsync(
                candidateIds, previousAuthority.Id, cancellationToken);
            eligibleIds.IntersectWith(previousSignedIds);
        }

        // Exclude already approved
        var alreadyApprovedIds = await _repository.GetAlreadyApprovedPropertyIdsAsync(
            eligibleIds.ToList(), signAuthorityId, cancellationToken);
        eligibleIds.ExceptWith(alreadyApprovedIds);

        return candidates
            .Where(p => eligibleIds.Contains(p.PropertyId))
            .ToList();
    }

    /// <summary>
    /// Identifies properties that violate sequential approval rule.
    /// </summary>
    private async Task<List<int>> GetSequenceViolationIdsAsync(
        List<int> propertyIds,
        int signAuthorityId,
        CancellationToken cancellationToken)
    {
        if (!propertyIds.Any())
            return new List<int>();

        var authorities = await _repository.GetAuthoritiesAsync(cancellationToken);
        var currentAuthority = authorities.FirstOrDefault(a => a.Id == signAuthorityId);

        if (currentAuthority == null)
            return propertyIds.Distinct().ToList();

        if (currentAuthority.SequenceOrder <= 1)
            return new List<int>();

        var previousAuthority = authorities
            .Where(a => a.SequenceOrder < currentAuthority.SequenceOrder)
            .OrderByDescending(a => a.SequenceOrder)
            .FirstOrDefault();

        if (previousAuthority == null)
            return propertyIds.Distinct().ToList();

        var previouslySignedIds = await _repository.GetSignedPropertyIdsAsync(
            propertyIds, previousAuthority.Id, cancellationToken);

        return propertyIds
            .Except(previouslySignedIds)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Parses Excel rows into approval items with validation.
    /// </summary>
    private (List<PropertyApprovalItemDto> Approvals, List<RejectedPropertyDto> Rejected) ParseExcelRows(
        IReadOnlyList<ExcelRow> rows)
    {
        var approvals = new List<PropertyApprovalItemDto>();
        var rejected = new List<RejectedPropertyDto>();
        var seenPropertyIds = new HashSet<int>();

        foreach (var row in rows)
        {
            var propertyIdText = GetCellValue(row.Cells, "PropertyId", "Property Id");
            var remarks = GetCellValue(row.Cells, "Remarks", "Remark");

            if (string.IsNullOrWhiteSpace(propertyIdText))
            {
                rejected.Add(new RejectedPropertyDto
                {
                    PropertyId = 0,
                    Reason = $"Row {row.RowNumber}: PropertyId is required."
                });
                continue;
            }

            if (!int.TryParse(propertyIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var propertyId)
                || propertyId <= 0)
            {
                rejected.Add(new RejectedPropertyDto
                {
                    PropertyId = 0,
                    Reason = $"Row {row.RowNumber}: PropertyId must be a positive integer."
                });
                continue;
            }

            if (!seenPropertyIds.Add(propertyId))
            {
                rejected.Add(new RejectedPropertyDto
                {
                    PropertyId = propertyId,
                    Reason = $"Row {row.RowNumber}: duplicate PropertyId in the uploaded file."
                });
                continue;
            }

            approvals.Add(new PropertyApprovalItemDto
            {
                PropertyId = propertyId,
                Remarks = remarks
            });
        }

        return (approvals, rejected);
    }

    /// <summary>
    /// Maps property IDs to rejected DTOs using AutoMapper.
    /// </summary>
    private List<RejectedPropertyDto> MapRejectedProperties(IEnumerable<int> propertyIds, string reason)
        => _mapper.Map<List<RejectedPropertyDto>>(
            propertyIds.ToList(),
            opt => opt.Items[ReasonKey] = reason);

    /// <summary>
    /// Maps pending export source row to export DTO using AutoMapper.
    /// </summary>
    private PropertySignaturePendingExportDto MapPendingExportRow(
        PropertySignaturePendingExportSourceDto row,
        PropertySignaturePendingExportAuthorityDto currentAuthority)
        => _mapper.Map<PropertySignaturePendingExportDto>(row, opt =>
        {
            opt.Items[PendingSignAtKey] = currentAuthority.AuthorityName;
            opt.Items[PendingOfficerNameKey] = currentAuthority.OfficerName;
        });

    /// <summary>
    /// Gets cell value from Excel row with flexible header matching.
    /// </summary>
    private static string? GetCellValue(IReadOnlyDictionary<string, string?> cells, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (cells.TryGetValue(key, out var value))
                return value;

            var normalizedKey = NormalizeHeader(key);
            var found = cells.FirstOrDefault(kvp => NormalizeHeader(kvp.Key) == normalizedKey);
            if (!string.IsNullOrEmpty(found.Key))
                return found.Value;
        }

        return null;
    }

    /// <summary>
    /// Normalizes header text for flexible matching.
    /// </summary>
    private static string NormalizeHeader(string value)
        => new string(value.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();

    /// <summary>
    /// Builds user-friendly approval message.
    /// </summary>
    private static string BuildApprovalMessage(int approvedCount, int rejectedCount)
    {
        if (approvedCount > 0 && rejectedCount > 0)
            return $"{approvedCount} property approval(s) saved successfully, {rejectedCount} rejected.";
        if (approvedCount > 0)
            return $"{approvedCount} property approval(s) saved successfully.";
        if (rejectedCount > 0)
            return $"No approvals were saved. {rejectedCount} property/properties were rejected.";
        return "No approvals were saved.";
    }

    #endregion
}
