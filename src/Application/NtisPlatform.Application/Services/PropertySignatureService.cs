using AutoMapper;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using System.Globalization;
using ClosedXML.Excel;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for property sign-off operations.
/// Owns all business rules:
///   1. Sequential validation — property must be approved by Authority N-1 before N.
///   2. Duplicate check — property not already approved by the same authority.
/// Delegates data access to IPropertySignatureRepository.
/// </summary>
public class PropertySignatureService : IPropertySignatureService
{
    private const string SequenceViolationReason = "Property has not been approved by the previous signing authority.";
    private const string AlreadyApprovedReason = "Property is already approved by this signing authority.";
    private const string ReasonKey = "Reason";
    private const string PendingSignAtKey = "PendingSignAt";
    private const string PendingOfficerNameKey = "PendingOfficerName";

    private readonly IPropertySignatureRepository _repository;
    private readonly IExcelUploadService _excelUploadService;
    private readonly IMapper _mapper;

    public PropertySignatureService(
        IPropertySignatureRepository repository,
        IExcelUploadService excelUploadService,
        IMapper mapper)
    {
        _repository = repository;
        _excelUploadService = excelUploadService;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────
    // 1. Authorities Lookup (pass-through — no business logic needed)
    // ─────────────────────────────────────────────────────

    public Task<List<SignAuthorityDto>> GetAuthoritiesAsync(
        CancellationToken cancellationToken = default)
        => _repository.GetAuthoritiesAsync(cancellationToken);

    // ─────────────────────────────────────────────────────
    // 2. Eligible Properties (pass-through — rule applied in repo query)
    // ─────────────────────────────────────────────────────

    public Task<List<EligiblePropertyDto>> GetEligiblePropertiesAsync(
        int signAuthorityId,
        int? zoneId,
        int? wardId,
        CancellationToken cancellationToken = default)
        => GetEligiblePropertiesCoreAsync(signAuthorityId, zoneId, wardId, cancellationToken);

    // ─────────────────────────────────────────────────────
    // 3. Submit Approvals — Main Business Logic
    // ─────────────────────────────────────────────────────

    public async Task<SubmitSignatureResponseDto> SubmitApprovalsAsync(
        int userId,
        SubmitSignatureRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var rejected = new List<RejectedPropertyDto>();
        var toApprove = new List<PropertyApprovalItemDto>();

        var requestedPropertyIds = request.PropertyApprovals.Select(p => p.PropertyId).ToList();

        // --- Business Rule 1: Sequential Check ---
        // Properties not yet approved by the previous authority are rejected.
        var sequenceViolations = await GetSequenceViolationIdsAsync(
            requestedPropertyIds, request.SignAuthorityId, cancellationToken);

        rejected.AddRange(MapRejectedProperties(sequenceViolations, SequenceViolationReason));

        // --- Business Rule 2: Duplicate Check ---
        // Properties already approved by this authority are rejected.
        var eligibleIds = requestedPropertyIds.Except(sequenceViolations).ToList();

        if (eligibleIds.Any())
        {
            var alreadyApproved = await _repository.GetAlreadyApprovedPropertyIdsAsync(
                eligibleIds, request.SignAuthorityId, cancellationToken);

            rejected.AddRange(MapRejectedProperties(alreadyApproved, AlreadyApprovedReason));

            // Final list to save
            var finalIds = eligibleIds.Except(alreadyApproved).ToHashSet();
            toApprove = request.PropertyApprovals
                .Where(p => finalIds.Contains(p.PropertyId))
                .ToList();
        }

        // --- Save valid approvals ---
        int savedCount = 0;
        if (toApprove.Any())
            savedCount = await _repository.SaveApprovalsAsync(
                userId, request.SignAuthorityId, toApprove, cancellationToken);

        return new SubmitSignatureResponseDto
        {
            ApprovedCount = savedCount,
            RejectedProperties = rejected,
            Message = savedCount > 0
                ? $"{savedCount} property approval(s) saved successfully."
                : "No approvals were saved."
        };
    }

    public async Task<PropertySignatureExcelUploadResultDto> UploadApprovalsFromExcelAsync(
        int userId,
        int signAuthorityId,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var (headers, rows) = _excelUploadService.Read(fileStream);
        var missingHeaders = _excelUploadService.GetMissingRequiredHeaders(
            headers,
            new[] { "PropertyId" });

        if (missingHeaders.Count > 0)
            throw new ArgumentException($"Missing required column(s): {string.Join(", ", missingHeaders)}.");

        var parseRejected = new List<RejectedPropertyDto>();
        var approvals = new List<PropertyApprovalItemDto>();
        var seenPropertyIds = new HashSet<int>();

        foreach (var row in rows)
        {
            var propertyIdText = GetCellValue(row.Cells, "PropertyId", "Property Id");
            var remarks = GetCellValue(row.Cells, "Remarks", "Remark");

            if (string.IsNullOrWhiteSpace(propertyIdText))
            {
                parseRejected.Add(new RejectedPropertyDto
                {
                    PropertyId = 0,
                    Reason = $"Row {row.RowNumber}: PropertyId is required."
                });
                continue;
            }

            if (!int.TryParse(propertyIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var propertyId)
                || propertyId <= 0)
            {
                parseRejected.Add(new RejectedPropertyDto
                {
                    PropertyId = 0,
                    Reason = $"Row {row.RowNumber}: PropertyId must be a positive integer."
                });
                continue;
            }

            if (!seenPropertyIds.Add(propertyId))
            {
                parseRejected.Add(new RejectedPropertyDto
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

        if (approvals.Count == 0)
        {
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

    public Task<byte[]> GetApprovalUploadTemplateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("PropertySignatureUpload");

        sheet.Cell(1, 1).Value = "PropertyId";
        sheet.Cell(1, 2).Value = "Remarks";

        // Example row for user guidance; user can remove it before upload.
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

    // ─────────────────────────────────────────────────────
    // 4. My Approvals (pass-through)
    // ─────────────────────────────────────────────────────

    public Task<List<SignatureApprovalDto>> GetMyApprovalsAsync(
        int userId,
        int signAuthorityId,
        int? zoneId,
        CancellationToken cancellationToken = default)
        => _repository.GetMyApprovalsAsync(userId, signAuthorityId, zoneId, cancellationToken);

    // ─────────────────────────────────────────────────────
    // 5. Property Status (pass-through)
    // ─────────────────────────────────────────────────────

    public Task<PropertySignatureStatusDto?> GetPropertySignatureStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
        => _repository.GetPropertySignatureStatusAsync(propertyId, cancellationToken);

    // ─────────────────────────────────────────────────────
    // 6. Revoke Approval (pass-through)
    // ─────────────────────────────────────────────────────

    public Task<bool> RevokeApprovalAsync(
        int propertyId,
        int signAuthorityId,
        int updatedBy,
        CancellationToken cancellationToken = default)
        => _repository.RevokeApprovalAsync(propertyId, signAuthorityId, updatedBy, cancellationToken);

    public Task<SignAuthorityGridResponseDto> GetSignAuthorityGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
        => _repository.GetSignAuthorityGridDataAsync(searchRequest, cancellationToken);

    public Task<SignAuthorityGridResponseDto> GetSignAuthorityWardGridDataAsync(
        int zoneId,
        CancellationToken cancellationToken = default)
        => _repository.GetSignAuthorityWardGridDataAsync(zoneId, cancellationToken);

    public Task<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>> GetSubGridAsync(
        int wardId,
        int workflowStageId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => _repository.GetBuildingWiseDataAsync(wardId, workflowStageId, pageNumber, pageSize, cancellationToken);

    public Task<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>> GetPropertyWiseDataAsync(
        string propertyNo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => _repository.GetPropertyWiseDataAsync(propertyNo, pageNumber, pageSize, cancellationToken);

    public async Task<List<PropertySignaturePendingExportDto>> GetPendingExportDataAsync(
        int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        var authorities = await _repository.GetPendingExportAuthoritiesAsync(cancellationToken);
        var currentAuthority = authorities.FirstOrDefault(a => a.SignAuthorityId == signAuthorityId);

        if (currentAuthority == null)
            return new List<PropertySignaturePendingExportDto>();

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

    private List<RejectedPropertyDto> MapRejectedProperties(IEnumerable<int> propertyIds, string reason)
        => _mapper.Map<List<RejectedPropertyDto>>(propertyIds.ToList(), opt => opt.Items[ReasonKey] = reason);

    private PropertySignaturePendingExportDto MapPendingExportRow(
        PropertySignaturePendingExportSourceDto row,
        PropertySignaturePendingExportAuthorityDto currentAuthority)
        => _mapper.Map<PropertySignaturePendingExportDto>(row, opt =>
        {
            opt.Items[PendingSignAtKey] = currentAuthority.AuthorityName;
            opt.Items[PendingOfficerNameKey] = currentAuthority.OfficerName;
        });

    private async Task<List<EligiblePropertyDto>> GetEligiblePropertiesCoreAsync(
        int signAuthorityId,
        int? zoneId,
        int? wardId,
        CancellationToken cancellationToken)
    {
        var authorities = await _repository.GetAuthoritiesAsync(cancellationToken);
        var currentAuthority = authorities.FirstOrDefault(a => a.Id == signAuthorityId);

        if (currentAuthority == null)
            return new List<EligiblePropertyDto>();

        var candidates = await _repository.GetEligiblePropertiesAsync(signAuthorityId, zoneId, wardId, cancellationToken);
        var candidateIds = candidates.Select(p => p.PropertyId).Distinct().ToList();

        if (!candidateIds.Any())
            return candidates;

        var eligibleIds = candidateIds.ToHashSet();

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

        var alreadyApprovedIds = await _repository.GetAlreadyApprovedPropertyIdsAsync(
            eligibleIds.ToList(), signAuthorityId, cancellationToken);
        eligibleIds.ExceptWith(alreadyApprovedIds);

        return candidates
            .Where(p => eligibleIds.Contains(p.PropertyId))
            .ToList();
    }

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

    private static string NormalizeHeader(string value)
        => new string(value.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();
}
