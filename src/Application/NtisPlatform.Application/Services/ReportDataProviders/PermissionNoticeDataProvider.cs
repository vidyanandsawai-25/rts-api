using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Application.Services.ReportDataProviders;

public class PermissionNoticeDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";
    public const string DetailSection = "CollectionReport";
    public const string ImageParamsSection = "_reportImageParams";
    public string ProviderCode => "PermissionNoticeDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<WingEntity> _wingRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;

    public PermissionNoticeDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<WingEntity> wingRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository)
    {
        _propertyRepository = propertyRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _wingRepository = wingRepository;
        _yearRepository = yearRepository;
        _ulbMasterRepository = ulbMasterRepository;
    }

    public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
    {
            new ReportSectionDescriptor(ImageParamsSection, false),
            new ReportSectionDescriptor(MainSection, false),
            new ReportSectionDescriptor(DetailSection, true),
        };

    public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var (rows, _) = await BuildPageAsync(parameters, 0, int.MaxValue, ct);
        return rows;
    }

    public async Task<ReportDataPage> GetDataPageAsync(
    Dictionary<string, string> parameters,
    string section,
    int page,
    int pageSize,
    CancellationToken ct = default)
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
            var (rows, hasMore) = await BuildPageAsync(parameters, (page - 1) * pageSize, pageSize, ct);

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

    private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        var activeYearId = await _yearRepository.GetQueryable()
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        // ---------------- PROPERTY QUERY ----------------
        var propQuery =
            from pm in _propertyRepository.GetQueryable()

            join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

            join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wmj
            from wm in wmj.DefaultIfEmpty()

            join sdm in _societyRepository.GetQueryable() on pm.Id equals sdm.PropertyId into sdmj
            from sdm in sdmj.DefaultIfEmpty()

            from ulb in _ulbMasterRepository.GetQueryable()
                .Where(x => x.IsActive)
                .Take(1)


            where pm.IsActive
                  && (zoneId == 0 || wm.ZoneId == zoneId)
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)

            orderby pm.PropertyNo, pm.PartitionNo

            select new
            {
                pm.Id,
                pm.OwnerName,
                pm.Address,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.PlotNo,
                pm.Location,
                zm.Description,
                wm.WardNo,
                sdm.SocietyName,

                // ----------------ULB----------------
                ulb.UlbName,
                ulb.UlbAddress,
                ulb.State,
                ulb.EmailId,
                ulb.MobileNo
            };

        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;

        var props = await propQuery.Skip(skip).Take(takePlusOne).ToListAsync(ct);

        var hasMore = take != int.MaxValue && props.Count > take;
        if (hasMore) props = props.Take(take).ToList();

        var ids = props.Select(x => x.Id).ToList();

        // ---------------- FINAL ROW ----------------
        var rows = props.Select(p =>
        {
            var row = new Dictionary<string, object?>
            {
                ["OwnerId"] = p.Id,
                ["MarathiOwnerName"] = p.OwnerName,
                ["MarathiOwnerAddress"] = p.Address,
                ["PropertyNo"] = p.PropertyNo,
                ["PartitionNo"] = p.PartitionNo,
                ["PlotNo"] = p.PlotNo,
                ["Location"] = p.Location,

                ["NodeDescription"] = p.Description,
                ["wardNo"] = p.WardNo,
                ["MarathiSocietyName"] = p.SocietyName,

                // ---------------- ULB ----------------
                ["CouncilName"] = p.UlbName,
                ["CouncilAddress"] = p.UlbAddress,
                ["CouncilState"] = p.State,
                ["CouncilEmailId"] = p.EmailId,
                ["CouncilMobileNo"] = p.MobileNo,

                ["zoneId"] = " ",
                ["wardId"] = " ",
            };

            return (object)row;
        }).ToList();

        return (rows, hasMore);
    }
}
