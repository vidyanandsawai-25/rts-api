using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;

public class SocietyOutstandingReportDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";

    public string ProviderCode => "SocietyOutstandingReportDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<PropertyMastOldEntity> _propertyMastOldRepository;
    private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
    private readonly IReportDataRepository<TaxPendingDetailsEntity> _taxPendingRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
    private readonly IReportDataRepository<PropertyMapDetailEntity> _propertyMapDetailRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _PropertyTypeMasterRepository;

    public SocietyOutstandingReportDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<PropertyMastOldEntity> propertyMastOldRepository,
        IReportDataRepository<TransMastEntity> transmastRepository,
        IReportDataRepository<TaxPendingDetailsEntity> taxPendingRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<PropertyMapDetailEntity> propertyMapDetailRepository,
        IReportDataRepository<PropertyTypeMasterEntity> PropertyTypeMasterRepository
        )
    {
        _propertyRepository = propertyRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _transmastRepository = transmastRepository;
        _taxPendingRepository = taxPendingRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _userRepository = userRepository;
        _yearRepository = yearRepository;
        _transRepository = transRepository;
        _ReportRequestRepository = reportRequestRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _PropertyTypeMasterRepository = PropertyTypeMasterRepository;
    }

    // Static — never runs a query (avoids any heavy query executing on the authenticate request).
    public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
    {
        new ReportSectionDescriptor(MainSection, false),
    };

    public async Task<object> GetDataAsync(
        Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, skip: 0, take: int.MaxValue, ct);
        return rows;
    }

    public async Task<ReportDataPage> GetDataPageAsync(
        Guid reportRequestId,
        Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
    {
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
        if (!parameters.TryGetValue("financeYear", out var financeYearStr))
            parameters.TryGetValue("financeyear", out financeYearStr);
        short.TryParse(financeYearStr, out var financeYear);
        return financeYear;
    }
    private IQueryable<YearMasterEntity> BaseQuery(short financeYear) => _yearRepository.GetQueryable()
        .Where(b => financeYear == 0 ? b.IsActive : b.Year == financeYear);

    private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
        Guid reportRequestId,
        Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        var requestedFinanceYear = ParseFinanceYear(parameters);
        var financeYearInfo = await BaseQuery(requestedFinanceYear)
            .OrderByDescending(x => x.Year)
            .Select(x => new { x.Id, x.Year, x.YearCode })
            .FirstOrDefaultAsync(ct);

        if (financeYearInfo is null)
            throw new InvalidOperationException("No active or requested finance year was found for Society Outstanding Report.");

        var activeYearId = financeYearInfo.Id;
        var financeYearDisplay = string.IsNullOrWhiteSpace(financeYearInfo.YearCode)
            ? financeYearInfo.Year.ToString() : financeYearInfo.YearCode;

        // ------------------- Parse parameters ----------------
        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdStr);
        int.TryParse(wardIdStr, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("fromPropertyNo", out var fromPropertyNo);
        fromPropertyNo = string.IsNullOrWhiteSpace(fromPropertyNo) ? null : fromPropertyNo.Trim();
        parameters.TryGetValue("toPropertyNo", out var toPropertyNo);
        toPropertyNo = string.IsNullOrWhiteSpace(toPropertyNo) ? null : toPropertyNo.Trim();


        parameters.TryGetValue("assessmentStatus", out var assessmentStatusText);
        int.TryParse(assessmentStatusText, out var assessmentStatus);

        if (!parameters.TryGetValue("Type", out var type))
            parameters.TryGetValue("type", out type);
        type = string.IsNullOrWhiteSpace(type)
            ? null
            : type.Trim().ToUpper();

        parameters.TryGetValue("partitionNo", out var partitionNo);
        partitionNo = string.IsNullOrWhiteSpace(partitionNo) ? null : partitionNo.Trim();
        parameters.TryGetValue("propertyId", out var propertyIdStr);
        parameters.TryGetValue("ownerId", out var ownerIdStr);

        parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
        int.TryParse(propertyTypeIdText, out var propertyTypeId);

        if (!parameters.TryGetValue("PropertyDescription", out var propertyDescription))
            parameters.TryGetValue("propertyDescription", out propertyDescription);
        propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();


        // ownerId and propertyId are aliases for PropertyMast.Id.
        List<int>? filterPropertyIds = new[] { propertyIdStr, ownerIdStr }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (filterPropertyIds.Count == 0)
            filterPropertyIds = null;

        return await BuildRowsAsync(
            zoneId, wardId, propertyNoText, fromPropertyNo, type, toPropertyNo,
            filterPropertyIds, partitionNo, propertyDescription, propertyTypeId,
            reportRequestId, requestedFinanceYear, activeYearId, financeYearDisplay,
            assessmentStatus, skip, take, ct);
    }

    private async Task<(List<object> Rows, bool HasMore)> BuildRowsAsync(
        int zoneId,
        int wardId,
        string? propertyNoText,
        string? fromPropertyNo,
        string? type,
        string? toPropertyNo,
        List<int>? filterPropertyIds,
        string? partitionNo,
        string? propertyDescription,
        int propertyTypeId,
        Guid reportRequestId,
        short requestedFinanceYear,
        int activeYearId,
        string financeYearDisplay,
        int assessmentStatus,
        int skip,
        int take,
        CancellationToken ct)
    {
        // ── 1. Fetch matching properties ──────────────────────────────────────
        var propertyQuery = from p in _propertyRepository.GetQueryable()
                            join w in _wardRepository.GetQueryable() on p.WardId equals w.Id into wj
                            from w in wj.DefaultIfEmpty()
                                // ---------------- JOIN PropertyTypeMaster ----------------
                            join pt in _PropertyTypeMasterRepository.GetQueryable() on p.PropertyTypeId equals pt.Id into ptj
                            from pt in ptj.DefaultIfEmpty()
                            where p.IsActive && !p.MarkedForDeletion
                                  && (zoneId == 0 || w.ZoneId == zoneId)
                                  && (wardId == 0 || p.WardId == wardId)
                                  && (propertyNoText == null || p.PropertyNo == propertyNoText)
                                  && (filterPropertyIds == null || filterPropertyIds.Contains(p.Id))
                                  && (assessmentStatus == 0 || p.PropertyAssessmentStatusId == assessmentStatus)
                                  && (string.IsNullOrEmpty(type) || pt.Type == type)
                                  && (propertyTypeId == 0 || p.PropertyTypeId == propertyTypeId)
                                  && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)
                            select new { Property = p, PropertyType = pt, ZoneId = w != null ? w.ZoneId : 0 };

        // PartitionNo filter is optional
        if (!string.IsNullOrWhiteSpace(partitionNo))
            propertyQuery = propertyQuery.Where(pq => pq.Property.PartitionNo == partitionNo);

        // Project the required property fields before applying the range and page.
        var projectedProperties = propertyQuery
            .Select(pq => new
            {
                pq.Property.Id,
                pq.Property.PropertyNo,
                pq.Property.WardId,
                pq.ZoneId,
                pq.Property.PartitionNo,
                pq.Property.UPICId,
                pq.Property.SubZoneNo,
                pq.Property.MobileNo,
                pq.Property.OwnerTitle,
                pq.Property.OccupierTitle,
                pq.Property.OwnerName,
                pq.Property.OccupierName,
                pq.Property.Address,
                pq.Property.FlatOrShopNo,
                pq.Property.FlatOrShopName,
                Type = pq.PropertyType != null ? pq.PropertyType.Type : null,
                PropertyDescription = pq.PropertyType != null ? pq.PropertyType.PropertyDescription : null,
            })
            .Distinct();

        var hasFromRange = int.TryParse(fromPropertyNo, out var fromPropertyNumber);
        var hasToRange = int.TryParse(toPropertyNo, out var toPropertyNumber);

        var queryWithNumericPropertyNo = projectedProperties.Select(p => new
        {
            Data = p,
            NumericPropertyNo =
                p.PropertyNo != null &&
                p.PropertyNo.Trim() != "" &&
                !EF.Functions.Like(p.PropertyNo.Trim(), "%[^0-9]%")
                    ? (int?)Convert.ToInt32(p.PropertyNo.Trim())
                    : null,
        });

        if (hasFromRange)
        {
            queryWithNumericPropertyNo = queryWithNumericPropertyNo.Where(x =>
                x.NumericPropertyNo.HasValue &&
                x.NumericPropertyNo.Value >= fromPropertyNumber);
        }

        if (hasToRange)
        {
            queryWithNumericPropertyNo = queryWithNumericPropertyNo.Where(x =>
                x.NumericPropertyNo.HasValue &&
                x.NumericPropertyNo.Value <= toPropertyNumber);
        }

        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
        var properties = await queryWithNumericPropertyNo
            .OrderBy(x => x.NumericPropertyNo)
            .ThenBy(x => x.Data.PartitionNo)
            .Skip(skip)
            .Take(takePlusOne)
            .Select(x => x.Data)
            .ToListAsync(ct);

        var hasMore = take != int.MaxValue && properties.Count > take;
        if (hasMore)
            properties = properties.Take(take).ToList();

        // If no properties match the filters, return empty result set (no exception).
        if (!properties.Any())
            return (new List<object>(), false);

        // ── 2. Shared lookups (run once for the whole batch) ──────────────────

        // 2a. Ward — resolve WardNo map dynamically
        var uniqueWardIds = properties
            .Select(p => p.WardId)
            .Where(wid => wid > 0)
            .Distinct()
            .ToList();

        var wardMap = await _wardRepository.GetQueryable()
            .Where(w => uniqueWardIds.Contains(w.Id))
            .Select(w => new { w.Id, w.WardNo })
            .ToDictionaryAsync(w => w.Id, w => w.WardNo, ct);

        // 2b. ULB Master (single row)
        var ulb = await _ulbMasterRepository.GetQueryable()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Id)
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

        // 2c. User
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
                    u.Id,
                    u.UserName,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.UserCode,
                    u.Email,
                    u.MobileNo,
                })
                .FirstOrDefaultAsync(ct);

        // ── 3. Collect all property IDs for batch queries ─────────────────────
        var propertyIds = properties.Select(p => p.Id).ToList();

        // 3a. Society details — keyed by PropertyId (int? in SocietyDetailsEntity)
        //     A property can have multiple wing rows; take the first per PropertyId.
        var societies = await _societyRepository.GetQueryable()
            .Where(sd => sd.PropertyId.HasValue && propertyIds.Contains(sd.PropertyId.Value)
                && sd.IsActive && !sd.MarkedForDeletion)
            .OrderBy(sd => sd.Id)
            .Select(sd => new
            {
                sd.PropertyId,
                sd.WingId,
                sd.WingName,
                sd.SocietyName,
                sd.SocietyAddress,
            })
            .ToListAsync(ct);
        // GroupBy to handle duplicate PropertyId rows (multiple wings per property)
        var societyMap = societies
            .GroupBy(s => s.PropertyId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // 3b. Map new property IDs to old property IDs via PropertyMapDetail
        var propertyMappings = await _propertyMapDetailRepository.GetQueryable()
            .Where(pmd => pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value) && pmd.IsActive && pmd.IsCurrent && pmd.Status == "ACTIVE")
            .OrderBy(pmd => pmd.Id)
            .Select(pmd => new { pmd.PropertyIdNew, pmd.PropertyIdOld })
            .ToListAsync(ct);

        var newToOldIdMap = propertyMappings
            .Where(m => m.PropertyIdNew.HasValue && m.PropertyIdOld.HasValue)
            .GroupBy(m => m.PropertyIdNew!.Value)
            .ToDictionary(g => g.Key, g => g.First().PropertyIdOld!.Value);

        var oldIds = newToOldIdMap.Values.Distinct().ToList();

        var propertyOlds = oldIds.Any()
            ? await _propertyMastOldRepository.GetQueryable()
                .Where(o => oldIds.Contains(o.Id))
                .Select(o => new
                {
                    o.Id,
                    o.OldWardNo,
                    o.OldPropertyNo,
                    o.OldPartitionNo,
                })
                .ToListAsync(ct)
            : new List<dynamic>() as dynamic;
        var propertyOldMap = oldIds.Any()
            ? ((IEnumerable<dynamic>)propertyOlds).ToDictionary(o => (int)o.Id)
            : new Dictionary<int, dynamic>();

        // 3c. TransMast — SUM(TaxAmount) per PropertyId (IsActive = true)
        var currentTaxSums = await _transmastRepository.GetQueryable()
            .Where(tm => propertyIds.Contains(tm.PropertyId) && tm.FinanceYearId == activeYearId
                && tm.IsActive && !tm.MarkedForDeletion)
            .GroupBy(tm => tm.PropertyId)
            .Select(g => new { PropertyId = g.Key, Total = g.Sum(tm => tm.TaxAmount) })
            .ToListAsync(ct);
        var currentTaxMap = currentTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

        // 3d. TaxPendingDetails — SUM(PendingAmount) per PropertyId (IsActive = true)
        var pendingTaxSums = await _taxPendingRepository.GetQueryable()
            // Outstanding arrears intentionally span all pending years.
            .Where(tp => propertyIds.Contains(tp.PropertyId) && tp.IsActive && !tp.MarkedForDeletion && !tp.PendingFixed)
            .GroupBy(tp => tp.PropertyId)
            .Select(g => new { PropertyId = g.Key, Total = g.Sum(tp => tp.PendingAmount) ?? 0m })
            .ToListAsync(ct);
        var pendingTaxMap = pendingTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

        // ── 4. Assemble one row per property ──────────────────────────────────---
        var rows = new List<object>();

        foreach (var property in properties)
        {
            societyMap.TryGetValue(property.Id, out var society);
            dynamic? propertyOld = null;
            if (newToOldIdMap.TryGetValue(property.Id, out var oldId))
            {
                propertyOldMap.TryGetValue(oldId, out propertyOld);
            }
            // Crystal formulas require stable numeric values. Never emit null for
            // either amount, even when a property has no matching tax rows.
            var totalCurrentTax = currentTaxMap.GetValueOrDefault(property.Id, 0m);
            var totalPendingTax = pendingTaxMap.GetValueOrDefault(property.Id, 0m);
            var totalTax = totalCurrentTax + totalPendingTax;


            string? propWardNo = null;
            if (wardMap.ContainsKey(property.WardId))
            {
                propWardNo = wardMap[property.WardId];
            }

            var row = new Dictionary<string, object?>
            {
                // ── Property fields — keys match XSD element names exactly ──
                ["propertyId"] = property.Id,
                ["ownerId"] = property.Id,
                ["PropertyNo"] = property.PropertyNo,
                ["WardId"] = property.WardId,
                ["zoneId"] = property.ZoneId,
                ["WardNo"] = propWardNo,
                ["PartitionNo"] = property.PartitionNo,
                ["UPICId"] = property.UPICId,
                ["SubZoneNo"] = property.SubZoneNo,
                ["MobileNo"] = property.MobileNo,
                ["OwnerTitle"] = property.OwnerTitle,
                ["OccupierTitle"] = property.OccupierTitle,
                ["OwnerName"] = property.OwnerName,
                ["OccupierName"] = property.OccupierName,
                ["Address"] = property.Address,
                ["FlatOrShopNo"] = property.FlatOrShopNo,
                ["FlatOrShopName"] = property.FlatOrShopName,
                // ── Society details — keys match XSD element names exactly ──
                ["WingId"] = society?.WingId,
                ["WingName"] = society?.WingName,
                ["SocietyName"] = society?.SocietyName,
                ["SocietyAddress"] = society?.SocietyAddress,
                // ── Old property data — keys match XSD element names exactly ──
                ["oldWardNo"] = propertyOld?.OldWardNo,
                ["oldPropertyNo"] = propertyOld?.OldPropertyNo,
                ["oldPartitionNo"] = propertyOld?.OldPartitionNo,
                // ── Tax amounts — keys match XSD element names exactly ──
                ["totalCurrentTaxAmount"] = totalCurrentTax,
                ["totalPendingTaxAmount"] = totalPendingTax,
                ["TotalTax"] = totalTax,
                // ── User fields — keys match XSD element names exactly ──
                ["userId"] = user?.Id,
                ["userName"] = user?.UserName,
                ["firstName"] = user?.FirstName,
                ["middleName"] = user?.MiddleName,
                ["lastName"] = user?.LastName,
                ["userCode"] = user?.UserCode,
                ["userEmail"] = user?.Email,
                ["userMobileNo"] = user?.MobileNo,
                // ── ULB fields — keys match XSD element names exactly ──
                ["ulbCode"] = ulb?.UlbCode,
                ["ulbName"] = ulb?.UlbName,
                ["ulbNameLocal"] = ulb?.UlbNameLocal,
                ["ulbLogo"] = ulb?.UlbLogo,
                ["emailId"] = ulb?.EmailId,
                ["mobileNo"] = ulb?.MobileNo,
                ["alternateMobileNo"] = ulb?.AlternateMobileNo,
                ["websiteUrl"] = ulb?.WebsiteUrl,
                ["ulbAddress"] = ulb?.UlbAddress,
                ["state"] = ulb?.State,
                ["district"] = ulb?.District,
                ["pinCode"] = ulb?.PinCode,
                // ── Finance year ──
                ["yearCode"] = financeYearDisplay,
                ["financeYear"] = financeYearDisplay,
                // ── Type / Description ──
                ["type"] = property.Type,
                ["PropertyDescription"] = property.PropertyDescription,
                ["propertyDescription"] = property.PropertyDescription,
            };

            rows.Add(row);
        }

        return (rows, hasMore);
    }
}
