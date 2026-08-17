using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;

/// <summary>
/// JaptiNotice report data provider.
/// Same parameters as NoDueCertificateDataProvider with an additional WardNo
/// column fetched via JOIN: PTIS.PropertyMast.WardId = PTIS.WardMaster.Id.
/// </summary>
public class JaptiNoticeDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";

    public string ProviderCode => "JaptiNoticeDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearMastRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportDataRepository<TaxPendingDetailsEntity> _taxPendingRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;


    public JaptiNoticeDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportDataRepository<YearMasterEntity> yearMastRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportDataRepository<TaxPendingDetailsEntity> taxPendingRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository)
    {
        _propertyRepository = propertyRepository;
        _wardRepository = wardRepository;
        _userRepository = userRepository;
        _yearMastRepository = yearMastRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _yearRepository = yearRepository;
        _transRepository = transRepository;
        _taxPendingRepository = taxPendingRepository;
        _ReportRequestRepository = reportRequestRepository;
    }

    // Static — never runs a query (avoids any heavy query executing on the authenticate request).
    public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
    {
        new ReportSectionDescriptor(MainSection, false),
    };

    public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var financeYear = ParseFinanceYear(parameters);
        var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, skip: 0, take: int.MaxValue, ct);
        return rows;
    }


    public async Task<ReportDataPage> GetDataPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
    {
        var financeYear = ParseFinanceYear(parameters);
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 100;

        var (rows, hasMore) = await BuildPageAsync(reportRequestId, parameters, (page - 1) * pageSize, pageSize, ct);
        return new ReportDataPage
        {
            Section = MainSection,
            Page = page,
            PageSize = pageSize,
            TotalCount = -1,
            HasMore = hasMore,
            Rows = rows,
        };
    }
    private static short ParseFinanceYear(Dictionary<string, string> parameters)
    {
        parameters.TryGetValue("financeYear", out var financeYearStr);
        short.TryParse(financeYearStr, out var financeYear);
        return financeYear;
    }
    private IQueryable<YearMasterEntity> BaseQuery(short financeYear) => _yearRepository.GetQueryable()
    .Where(b => financeYear == 0 ? b.IsActive : b.Year == financeYear);

    private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        // --- Parse parameters ---
        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
        fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText)
            ? null
            : fromPropertyNoText.Trim();

        parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
        toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText)
            ? null
            : toPropertyNoText.Trim();



        parameters.TryGetValue("assessmentStatus", out var assessmentStatusText);
        int.TryParse(assessmentStatusText, out var assessmentStatus);

        var financeYear = ParseFinanceYear(parameters);
        int activeYearId = 0;
        if (financeYear != 0)
        {
            activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);
        }

        // ownerId is the property master id used by the report UI. Keep propertyId as a
        // backward-compatible alias for older callers.
        parameters.TryGetValue("ownerId", out var ownerIdStr);
        parameters.TryGetValue("propertyId", out var propertyIdStr);
        parameters.TryGetValue("userId", out var userIdStr);



        var requestedByUserId = await _ReportRequestRepository.GetQueryable()
        .Where(r => r.ReportRequestId == reportRequestId)
        .Select(r => (int?)r.RequestedByUserId)
        .FirstOrDefaultAsync(ct);

        var user = requestedByUserId == null
            ? null
            : await _userRepository.GetQueryable()
                .Where(u => u.Id == requestedByUserId.Value)
                .Select(u => new
                {
                    RequestedByUserId = requestedByUserId.Value,
                    u.Id,
                    u.UserName,
                    u.UserCode
                })
                .FirstOrDefaultAsync(ct);



        // Split on commas, parse each token, deduplicate, and drop invalid entries.
        // Match DocumentNotice semantics: ownerId takes precedence when supplied.
        var requestedPropertyIds = (!string.IsNullOrWhiteSpace(ownerIdStr)
                ? ownerIdStr
                : propertyIdStr ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        int.TryParse(userIdStr, out var userId);

        // --- 1. Active year ---
        var activeYear = await _yearMastRepository.GetQueryable()
            .Where(ym => ym.IsActive)
            .Select(ym => new { ym.Id, ym.Year, ym.YearCode })
            .FirstOrDefaultAsync(ct);

        // Resolve the final property ids through one common filter pipeline. This makes
        // ownerId, zone/ward, exact property, partition, assessment status, finance year,
        // and from/to property range work together just like DocumentNotice.
        var query =
            from p in _propertyRepository.GetQueryable()
            join w in _wardRepository.GetQueryable() on p.WardId equals w.Id into wj
            from w in wj.DefaultIfEmpty()
            where p.IsActive && !p.MarkedForDeletion
                  && (requestedPropertyIds.Count == 0 || requestedPropertyIds.Contains(p.Id))
                  && (zoneId == 0 || w.ZoneId == zoneId)
                  && (wardId == 0 || p.WardId == wardId)
                  && (propertyNoText == null || p.PropertyNo == propertyNoText)
                  && (partitionNoText == null || p.PartitionNo == partitionNoText)
                  && (assessmentStatus == 0 || p.PropertyAssessmentStatusId == assessmentStatus)
            select p;

        if (financeYear != 0 && activeYearId > 0)
        {
            var transQ = _transRepository.GetQueryable()
                .Where(t => t.FinanceYearId == activeYearId)
                .Select(t => t.PropertyId);

            query = query.Where(p => transQ.Contains(p.Id));
        }

        var queryResult = await query
            .Select(p => new { p.Id, p.PropertyNo })
            .Distinct()
            .ToListAsync(ct);

        // Apply the same numeric/non-numeric range behavior as DocumentNotice.
        if (int.TryParse(fromPropertyNoText, out var fromPropertyNo))
        {
            queryResult = queryResult
                .Where(x => int.TryParse(x.PropertyNo, out var no) && no >= fromPropertyNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(fromPropertyNoText))
        {
            queryResult = queryResult
                .Where(x => string.Compare(
                    x.PropertyNo,
                    fromPropertyNoText,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        if (int.TryParse(toPropertyNoText, out var toPropertyNo))
        {
            queryResult = queryResult
                .Where(x => int.TryParse(x.PropertyNo, out var no) && no <= toPropertyNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(toPropertyNoText))
        {
            queryResult = queryResult
                .Where(x => string.Compare(
                    x.PropertyNo,
                    toPropertyNoText,
                    StringComparison.OrdinalIgnoreCase) <= 0)
                .ToList();
        }

        var propertyIds = queryResult.Select(x => x.Id).ToList();

        // --- 2. All properties in one batch query ---
        var properties = await (
            from p in _propertyRepository.GetQueryable()
            where p.IsActive && !p.MarkedForDeletion
                  && propertyIds.Contains(p.Id)
            select new
            {
                p.Id,
                p.WardId,
                p.PropertyNo,
                p.PartitionNo,
                p.OwnerTitle,
                p.OwnerName,
                p.OwnerTitleEnglish,
                p.OwnerNameEnglish,
                p.OccupierTitle,
                p.OccupierName,
                p.OccupierTitleEnglish,
                p.OccupierNameEnglish,
                p.Address,
                p.AddressEnglish,
                assessmentStatus = p.PropertyAssessmentStatusId,
            })
            .Distinct()
            .ToListAsync(ct);

        // --- 3. Unique ward IDs — resolve WardNo one-by-one (avoids nullable-int JOIN crash) ---
        var uniqueWardIds = properties
            .Select(p => p.WardId)
            .Where(wid => wid > 0)
            .Distinct()
            .ToList();

        var wardMap = new Dictionary<int, string?>();
        foreach (var wid in uniqueWardIds)
        {
            var wardNo = await _wardRepository.GetQueryable()
                .Where(w => w.Id == wid)
                .Select(w => w.WardNo)
                .FirstOrDefaultAsync(ct);
            wardMap[wid] = wardNo;
        }

        // --- 5. ULB Master (single row, shared across all rows) ---
        var ulb = await _ulbMasterRepository.GetQueryable()
            .Select(u => new
            {
                u.UlbCode,
                u.UlbName,
                u.UlbNameLocal,
                u.UlbLogo,
                u.EmailId,
                u.MobileNo,
                u.AlternateMobileNo,
                u.WebsiteUrl,
                u.UlbAddress,
                u.State,
                u.District,
                u.PinCode,
            })
            .FirstOrDefaultAsync(ct);

        // --- Build one output row per property (preserves the requested order if propertyIds list is provided) ---
        var allRows = new List<Dictionary<string, object?>>();

        var propertiesToLoop = propertyIds.Count > 0
            ? propertyIds.Select(pid => properties.FirstOrDefault(p => p.Id == pid)).Where(p => p != null).ToList()
            : properties;

        // --- 5. Calculate TaxAmount and PendingAmount for each property ---
        var propertyIdsToQuery = propertiesToLoop.Select(p => p!.Id).ToList();
        var resolvedActiveYearId = activeYearId != 0 ? activeYearId : (activeYear?.Id ?? 0);

        // TransMast — SUM(TaxAmount) per PropertyId (IsActive = true)
        var currentTaxSums = await _transRepository.GetQueryable()
            .Where(tm => propertyIdsToQuery.Contains(tm.PropertyId) && tm.FinanceYearId == resolvedActiveYearId
                && tm.IsActive && !tm.MarkedForDeletion)
            .GroupBy(tm => tm.PropertyId)
            .Select(g => new { PropertyId = g.Key, Total = g.Sum(tm => tm.TaxAmount) })
            .ToListAsync(ct);
        var currentTaxMap = currentTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

        // TaxPendingDetails — SUM(PendingAmount) per PropertyId (IsActive = true)
        var pendingTaxSums = await _taxPendingRepository.GetQueryable()
            .Where(tp => propertyIdsToQuery.Contains(tp.PropertyId) && tp.IsActive && !tp.MarkedForDeletion && !tp.PendingFixed)
            .GroupBy(tp => tp.PropertyId)
            .Select(g => new { PropertyId = g.Key, Total = g.Sum(tp => tp.PendingAmount) ?? 0m })
            .ToListAsync(ct);
        var pendingTaxMap = pendingTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

        foreach (var property in propertiesToLoop)
        {
            wardMap.TryGetValue(property!.WardId, out var wardNo);

            var taxAmount = currentTaxMap.GetValueOrDefault(property.Id, 0m);
            var pendingAmount = pendingTaxMap.GetValueOrDefault(property.Id, 0m);
            var totalDemand = taxAmount + pendingAmount;

            allRows.Add(new Dictionary<string, object?>
            {
                // Active year fields
                ["activeYearId"] = activeYear?.Id,
                ["year"] = activeYear?.Year,
                ["yearCode"] = activeYear?.YearCode,
                // Property fields
                ["propertyId"] = property.Id,
                ["wardId"] = property.WardId,
                ["wardNo"] = wardNo,
                ["propertyNo"] = property.PropertyNo,
                ["partitionNo"] = property.PartitionNo,
                ["ownerTitle"] = property.OwnerTitle,
                ["ownerName"] = property.OwnerName,
                ["ownerTitleEnglish"] = property.OwnerTitleEnglish,
                ["ownerNameEnglish"] = property.OwnerNameEnglish,
                ["occupierTitle"] = property.OccupierTitle,
                ["occupierName"] = property.OccupierName,
                ["occupierTitleEnglish"] = property.OccupierTitleEnglish,
                ["occupierNameEnglish"] = property.OccupierNameEnglish,
                ["address"] = property.Address,
                // Tax amount fields
                ["taxAmount"] = taxAmount,
                ["pendingAmount"] = pendingAmount,
                ["totalDemand"] = totalDemand,
                // User fields
                ["userId"] = user?.Id,
                ["userName"] = user?.UserName,
                ["userCode"] = user?.UserCode,
                // ULB Master fields
                ["ulbCode"] = ulb?.UlbCode,
                ["ulbName"] = ulb?.UlbName,
                ["ulbNameLocal"] = ulb?.UlbNameLocal,
                ["ulbLogo"] = ulb?.UlbLogo,
                ["ulbEmailId"] = ulb?.EmailId,
                ["ulbMobileNo"] = ulb?.MobileNo,
                ["ulbAlternateMobileNo"] = ulb?.AlternateMobileNo,
                ["ulbWebsiteUrl"] = ulb?.WebsiteUrl,
                ["ulbAddress"] = ulb?.UlbAddress,
                ["ulbState"] = ulb?.State,
                ["ulbDistrict"] = ulb?.District,
                ["ulbPinCode"] = ulb?.PinCode,
                ["financeYear"] = "",
                ["assessmentStatus"] = "",
            });
        }

        // Apply skip/take so the paged overload works correctly.
        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
        var paged = allRows.Skip(skip).Take(takePlusOne).ToList();
        var hasMore = take != int.MaxValue && paged.Count > take;
        if (hasMore) paged = paged.Take(take).ToList();

        return (paged.Cast<object>().ToList(), hasMore);
    }
}

