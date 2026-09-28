using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;

public class BlankHearingFormatDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";
    public const string DetailSection = "CollectionReport";
    public const string ImageParamsSection = "_reportImageParams";
    public string ProviderCode => "BlankHearingFormatDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<PropertyMastOldEntity> _PropertyMastOldRepository;
    private readonly IReportDataRepository<TransMastOldEntity> _transMastOldRepository;
    private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<PropertyMapMasterEntity> _PropertyMapRepository;
    private readonly IReportDataRepository<PropertyMapDetailEntity> _PropertyMapDetailRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _PropertyTypeMasterRepository;

    public BlankHearingFormatDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<PropertyMastOldEntity> PropertyMastOldRepository,
        IReportDataRepository<TransMastOldEntity> transMastOldRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<PropertyMapMasterEntity> PropertyMapRepository,
        IReportDataRepository<PropertyMapDetailEntity> PropertyMapDetailRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportDataRepository<PropertyTypeMasterEntity> PropertyTypeMasterRepository)
    {
        _propertyRepository = propertyRepository;
        _PropertyMastOldRepository = PropertyMastOldRepository;
        _transMastOldRepository = transMastOldRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _PropertyMapRepository = PropertyMapRepository;
        _PropertyMapDetailRepository = PropertyMapDetailRepository;
        _ReportRequestRepository = reportRequestRepository;
        _userRepository = userRepository;
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

    private IQueryable<YearMasterEntity> BaseQuery(short financeYear) =>
    _yearRepository.GetQueryable()
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

        parameters.TryGetValue("Type", out var type);
        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToUpper();

        parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
        int.TryParse(propertyTypeIdText, out var propertyTypeId);

        parameters.TryGetValue("PropertyDescription", out var propertyDescription);
        propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();

        var financeYear = ParseFinanceYear(parameters);
        var activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);

        var transOldAggQuery =
            from tmo in _transMastOldRepository.GetQueryable()
            where tmo.IsActive && !tmo.MarkedForDeletion
            group tmo by tmo.PropertyMastOldId into g
            select new
            {
                PropertyMastOldId = g.Key,
                OldTotalTax = g.Sum(x => (decimal?)x.TaxAmount) ?? 0m
            };

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

            //join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj     
            //from zm in zmj.DefaultIfEmpty()

            join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wmj
            from wm in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on wm.ZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

            join sdm in _societyRepository.GetQueryable() on pm.Id equals sdm.PropertyId into sdmj
            from sdm in sdmj.DefaultIfEmpty()

                //----------- ProperyMastOld join --------------
            join pmd in _PropertyMapDetailRepository.GetQueryable() on pm.Id equals pmd.PropertyIdNew into pmdj
            from pmd in pmdj.DefaultIfEmpty()

            join pmm in _PropertyMapRepository.GetQueryable() on pmd.PropertyMapId equals pmm.Id into pmmj
            from pmm in pmmj.DefaultIfEmpty()

            join pmo in _PropertyMastOldRepository.GetQueryable() on pmd.PropertyIdOld equals pmo.Id into oldj
            from pmo in oldj.DefaultIfEmpty()

                // ---------------- NEW: Join Transaction Master for Finance Year filterin
            join tmoa in transOldAggQuery
                on pmo.Id equals tmoa.PropertyMastOldId into tmoaj
            from tmoa in tmoaj.DefaultIfEmpty()

            from ulb in _ulbMasterRepository.GetQueryable()
                .Where(x => x.IsActive)
                .Take(1)

            where pm.IsActive
                  && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
                  && (zoneId == 0 || wm.ZoneId == zoneId)
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)
                  && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)
                   && (string.IsNullOrEmpty(type) || pt.Type == type)
                  && (propertyTypeId == 0 || pt.Id == propertyTypeId)
                  && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)

            orderby pm.PropertyNo, pm.PartitionNo

            select new
            {
                pm.Id,
                pm.Address,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.PlotNo,
                pm.Location,
                zm.Description,
                wm.WardNo,
                sdm.SocietyName,
                pmo.OldWardNo,
                pmo.OldPropertyNo,
                pm.OccupierName,
                pm.FlatOrShopNo,

                // ----------------ULB----------------
                ulb.UlbName,
                ulb.UlbAddress,
                ulb.EmailId,
                ulb.MobileNo
            };


        var props = await propQuery.Distinct().ToListAsync(ct);

        // ---------------- FROM Property - TO Property Filter ----------------
        if (int.TryParse(fromPropertyNoText, out var fromPropertyNo))
        {
            props = props
                .Where(x =>
                    int.TryParse(x.PropertyNo, out var no)
                    && no >= fromPropertyNo)
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

        if (int.TryParse(toPropertyNoText, out var toPropertyNo))
        {
            props = props
                .Where(x =>
                    int.TryParse(x.PropertyNo, out var no)
                    && no <= toPropertyNo)
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

        // ---------------- PAGINATION ----------------
        var takePlusOne = take == int.MaxValue
            ? int.MaxValue
            : take + 1;

        props = props
            .Skip(skip)
            .Take(takePlusOne)
            .ToList();

        var hasMore = take != int.MaxValue && props.Count > take;

        if (hasMore)
        {
            props = props.Take(take).ToList();
        }

        var ids = props.Select(x => x.Id).ToList();

        // ---------------- FINAL ROW ----------------
        var rows = props.Select(p =>
        {
            var row = new Dictionary<string, object?>
            {
                ["OwnerId"] = p.Id,
                ["MarathiOwnerAddress"] = p.Address,
                ["PropertyNo"] = p.PropertyNo,
                ["PartitionNo"] = p.PartitionNo,
                ["PlotNo"] = p.PlotNo,
                ["Location"] = p.Location,

                ["NodeDescription"] = p.Description,
                ["wardNo"] = p.WardNo,
                ["MarathiSocietyName"] = p.SocietyName,
                ["OldWardNo"] = p.OldWardNo,
                ["OldPropertyNo"] = p.OldPropertyNo,
                ["OccupierName"] = p.OccupierName,
                ["FlatOrShopNo"] = p.FlatOrShopNo,

                // ---------------- ULB ----------------
                ["CouncilName"] = p.UlbName,
                ["CouncilAddress"] = p.UlbAddress,
                ["CouncilEmailId"] = p.EmailId,
                ["CouncilMobileNo"] = p.MobileNo,
                ["userName"] = user?.UserName,

                ["zoneId"] = " ",
                ["wardId"] = " ",
            };

            return (object)row;
        }).ToList();

        return (rows, hasMore);
    }
}

