using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;

public class DocumentNoticeDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";
    public const string DetailSection = "CollectionReport";
    public const string ImageParamsSection = "_reportImageParams";
    public string ProviderCode => "DocumentNoticeDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _PropertyTypeMasterRepository;

    public DocumentNoticeDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportDataRepository<PropertyTypeMasterEntity> PropertyTypeMasterRepository)
    {
        _propertyRepository = propertyRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _userRepository = userRepository;
        _ReportRequestRepository = reportRequestRepository;
        _yearRepository = yearRepository;
        _transRepository = transRepository;
        _PropertyTypeMasterRepository = PropertyTypeMasterRepository;
    }

    public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
    {
            new ReportSectionDescriptor(ImageParamsSection, false),
            new ReportSectionDescriptor(MainSection, false),
            new ReportSectionDescriptor(DetailSection, true),
        };

    public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, 0, int.MaxValue, ct);
        return rows;
    }

    public async Task<ReportDataPage> GetDataPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 100;

        if (section.Equals(ImageParamsSection, StringComparison.OrdinalIgnoreCase))
        {
            return new ReportDataPage
            {
                Section = ImageParamsSection,
                Page = 1,
                PageSize = 1,
                TotalCount = 1,
                HasMore = false,
                Rows = new List<object>()
            };
        }

        if (section.Equals(MainSection, StringComparison.OrdinalIgnoreCase) ||
            section.Equals(DetailSection, StringComparison.OrdinalIgnoreCase))
        {
            var (rows, hasMore) = await BuildPageAsync(reportRequestId, parameters, (page - 1) * pageSize, pageSize, ct);

            return new ReportDataPage
            {
                Section = section,
                Page = page,
                PageSize = pageSize,
                TotalCount = -1,
                HasMore = hasMore,
                Rows = rows
            };
        }

        return new ReportDataPage
        {
            Section = section,
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            HasMore = false,
            Rows = new List<object>()
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
        parameters.TryGetValue("ownerId", out var ownerIdText);

        var ownerIds = string.IsNullOrWhiteSpace(ownerIdText) ? new List<int>() : ownerIdText
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        // ------ FROM Property - TO Property Number Range Filter Parameters ------
        parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
        fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText)
            ? null
            : fromPropertyNoText.Trim();

        parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
        toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText)
            ? null
            : toPropertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        parameters.TryGetValue("assessmentStatus", out var assessmentStatusText);
        int.TryParse(assessmentStatusText, out var assessmentStatus);

        // helper: case-insensitive parameter lookup
        static string? GetParam(IDictionary<string, string> p, string key)
        {
            if (p.TryGetValue(key, out var v)) return string.IsNullOrWhiteSpace(v) ? null : v;
            var kv = p.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value;
        }

        // use helper for these params
        var type = GetParam(parameters, "Type")?.Trim().ToUpper();

        var propertyTypeIdText = GetParam(parameters, "propertyTypeId");
        int.TryParse(propertyTypeIdText, out var propertyTypeId);

        var propertyDescription = GetParam(parameters, "PropertyDescription")?.Trim();

        var financeYear = ParseFinanceYear(parameters);
        var activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);

        // ---------------- GET USER INFO ----------------
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
                    u.UserName
                })
                .FirstOrDefaultAsync(ct);

        // ---------------- PROPERTY QUERY ----------------
        var propQuery =
            from pm in _propertyRepository.GetQueryable()
            join pt in _PropertyTypeMasterRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id

            join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wmj
            from wm in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on wm.ZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

                // pick a single society row per property (TOP(1) semantics) to avoid duplicates
            from sdm in _societyRepository.GetQueryable()
                .Where(s => s.PropertyId == pm.Id)
                .OrderBy(s => s.Id)
                .Take(1)
                .DefaultIfEmpty()
            from ulb in _ulbMasterRepository.GetQueryable()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .Take(1)
                .DefaultIfEmpty()

            where pm.IsActive
                  && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
                  && (zoneId == 0 || (wm != null && wm.ZoneId == zoneId))
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)
                  && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)
                  && (string.IsNullOrEmpty(type) || pt.Type == type)
                  && (propertyTypeId == 0 || pt.Id == propertyTypeId)
                  && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)
            select new
            {
                pm.Id,
                pm.OwnerName,
                pm.OccupierName,
                pm.Address,
                pm.PropertyNo,
                pm.PartitionNo,
                Description = zm.Description,
                WardNo = wm.WardNo,
                SocietyName = sdm.SocietyName,
                pm.FlatOrShopNo,
                pt.Type,
                pt.PropertyDescription,
                pm.PropertyAssessmentStatusId,
                UlbName = ulb.UlbName,
                UlbAddress = ulb.UlbAddress,
                UlbEmail = ulb.EmailId,
                UlbMobile = ulb.MobileNo
            };

        // Apply finance-year constraint server-side ONLY when caller provided a financeYear
        if (financeYear != 0 && activeYearId > 0)
        {
            var transQ = _transRepository.GetQueryable()
                .Where(t => t.FinanceYearId == activeYearId)
                .Select(t => t.PropertyId);
            propQuery = propQuery.Where(x => transQ.Contains(x.Id));
        }

        var props = await propQuery
    .Distinct()
    .OrderBy(x => x.PropertyNo)
    .ThenBy(x => x.PartitionNo)
    .ToListAsync(ct);

        // Apply FromPropertyNo & ToPropertyNo range filter
        if (int.TryParse(fromPropertyNoText, out var fromPropNo))
        {
            props = props
                .Where(x => int.TryParse(x.PropertyNo, out var no) && no >= fromPropNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(fromPropertyNoText))
        {
            props = props
                .Where(x => string.Compare(
                    x.PropertyNo,
                    fromPropertyNoText,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        if (int.TryParse(toPropertyNoText, out var toPropNo))
        {
            props = props
                .Where(x => int.TryParse(x.PropertyNo, out var no) && no <= toPropNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(toPropertyNoText))
        {
            props = props
                .Where(x => string.Compare(
                    x.PropertyNo,
                    toPropertyNoText,
                    StringComparison.OrdinalIgnoreCase) <= 0)
                .ToList();
        }

        // Pagination
        var takePlusOne = take == int.MaxValue
            ? int.MaxValue
            : take + 1;

        var fetched = props
            .Skip(skip)
            .Take(takePlusOne)
            .ToList();

        var hasMore = take != int.MaxValue && fetched.Count > take;

        if (hasMore)
        {
            fetched = fetched.Take(take).ToList();
        }

        // Map to rows (same as original)
        var rows = fetched.Select(p =>
        {
            var row = new Dictionary<string, object?>
            {
                ["ownerId"] = p.Id,
                ["OwnerName"] = p.OwnerName,
                ["OccupierName"] = p.OccupierName,
                ["MarathiOwnerAddress"] = p.Address,
                ["PropertyNo"] = p.PropertyNo,
                ["PartitionNo"] = p.PartitionNo,

                ["NodeDescription"] = p.Description,
                ["wardNo"] = p.WardNo,
                ["MarathiSocietyName"] = p.SocietyName,
                ["FlatOrShopNo"] = p.FlatOrShopNo,

                ["CouncilName"] = p.UlbName,
                ["CouncilAddress"] = p.UlbAddress,
                ["CouncilEmailId"] = p.UlbEmail,
                ["CouncilMobileNo"] = p.UlbMobile,
                ["userName"] = user?.UserName,

                ["zoneId"] = " ",
                ["wardId"] = " ",
            };

            return (object)row;
        }).ToList();

        return (rows, hasMore);
    }
}

