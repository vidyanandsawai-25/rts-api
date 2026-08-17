using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;

public class TypeWiseSurveyFormDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";
    public const string DetailSection = "CollectionReport";
    public const string ImageParamsSection = "_reportImageParams";
    public string ProviderCode => "TypeWiseSurveyFormDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
    private readonly IReportDataRepository<WardEntity> _ward_repository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _society_repository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<WingEntity> _wingRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;
    private readonly IReportDataRepository<RenterMastEntity> _renterMastRepository;
    private readonly IReportDataRepository<PropertyMastOldEntity> _propertyOldRepository;
    private readonly IReportDataRepository<DocumentEntity> _documentRepository;
    private readonly IReportDataRepository<DocumentBindingEntity> _documentBindingRepository;
    private readonly IReportDataRepository<PropertyPhotoEntity> _propertyPhotoRepository;

    public TypeWiseSurveyFormDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<WingEntity> wingRepository,
        IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository,
        IReportDataRepository<RenterMastEntity> renterMastRepository,
        IReportDataRepository<PropertyMastOldEntity> propertyOldRepository,
        IReportDataRepository<DocumentEntity> documentRepository,
        IReportDataRepository<DocumentBindingEntity> documentBindingRepository,
        IReportDataRepository<PropertyPhotoEntity> propertyPhotoRepository)
    {
        _propertyRepository = propertyRepository;
        _zoneRepository = zoneRepository;
        _ward_repository = wardRepository;
        _society_repository = societyRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _wingRepository = wingRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _renterMastRepository = renterMastRepository;
        _propertyOldRepository = propertyOldRepository;
        _documentRepository = documentRepository;
        _documentBindingRepository = documentBindingRepository;
        _propertyPhotoRepository = propertyPhotoRepository;
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

    private async Task<int> ResolvePropertyIdAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        if (parameters.TryGetValue("propertyId", out var propertyIdText) &&
            int.TryParse(propertyIdText, out var propertyId) &&
            propertyId > 0)
        {
            return propertyId;
        }

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        return await _propertyRepository.GetQueryable()
            .Where(pm => pm.IsActive
                && (wardId == 0 || pm.WardId == wardId)
                && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                && (partitionNoText == null || pm.PartitionNo == partitionNoText))
            .Select(pm => pm.Id)
            .FirstOrDefaultAsync(ct);
    }


    public async Task<ReportDataPage> GetDataPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 100;
        var financeYear = ParseFinanceYear(parameters);

        if (section.Equals(ImageParamsSection, StringComparison.OrdinalIgnoreCase))
        {
            var propertyId = await ResolvePropertyIdAsync(parameters, ct);
            parameters.TryGetValue("photoTypeId", out var photoTypeIdText);
            int.TryParse(photoTypeIdText, out var photoTypeId);

            var row = await (
                from pp in _propertyPhotoRepository.GetQueryable()
                join db in _documentBindingRepository.GetQueryable() on pp.DocumentBindingId equals db.Id
                join d in _documentRepository.GetQueryable() on db.DocumentId equals d.Id
                where pp.PropertyId == propertyId
                      && pp.IsLatest
                      && pp.IsActive
                      && !pp.MarkedForDeletion
                      && (photoTypeId == 0 || pp.PhotoTypeId == photoTypeId)
                      && db.IsActive
                      && !db.MarkedForDeletion
                      && d.IsActive
                      && !d.MarkedForDeletion
                      && d.MimeType.StartsWith("image/")
                select new { logo_imageGuid = d.DocumentGuid }
            ).FirstOrDefaultAsync(ct);

            return new ReportDataPage
            {
                Section = ImageParamsSection,
                Page = 1,
                PageSize = 1,
                TotalCount = row is null ? 0 : 1,
                HasMore = false,
                Rows = row is null ? new List<object>() : new List<object> { row }
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

    private static short ParseFinanceYear(Dictionary<string, string> parameters)
    {
        parameters.TryGetValue("financeYear", out var financeYearStr);
        short.TryParse(financeYearStr, out var financeYear);
        return financeYear;
    }

    private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        parameters.TryGetValue("ownerId", out var ownerIdText);

        var ownerIds = string.IsNullOrWhiteSpace(ownerIdText)
    ? new List<int>()
    : ownerIdText
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => int.TryParse(x, out var id) ? id : 0)
        .Where(id => id > 0)
        .Distinct()
        .ToList();

        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        // NEW: parse from/to property number, assessmentStatus, Type, PropertyDescription
        parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
        fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText) ? null : fromPropertyNoText.Trim();

        parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
        toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText) ? null : toPropertyNoText.Trim();

        parameters.TryGetValue("assessmentStatus", out var assessmentStatusText);
        int.TryParse(assessmentStatusText, out var assessmentStatus);

        parameters.TryGetValue("Type", out var type);
        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToUpperInvariant();

        parameters.TryGetValue("PropertyDescription", out var propertyDescription);
        propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();

        // --------------- ULB ---------------- 

        var ulb = await _ulbMasterRepository.GetQueryable().Where(x => x.IsActive).Select(x => new
        {
            CouncilName = x.UlbName,
            CouncilAddress = x.UlbAddress
        })
        .FirstOrDefaultAsync(ct);

        var rm = await _renterMastRepository.GetQueryable().Where(x => x.IsActive).Select(x => new
        {
            RenterName = x.RenterName,
            RentMonthly = x.RentMonthly
        })
            .FirstOrDefaultAsync(ct);


        // ---------------- PROPERTY QUERY ----------------
        var propQuery =
            from pm in _propertyRepository.GetQueryable()

            join wm in _ward_repository.GetQueryable() on pm.WardId equals wm.Id into wmj
            from wm in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on wm.ZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

                //join sdm in _society_repository.GetQueryable() on pm.Id equals sdm.PropertyId into sdmj
                //from sdm in sdmj.DefaultIfEmpty()
            from sdm in _society_repository.GetQueryable()
                .Where(s => s.PropertyId == pm.Id)
                .OrderBy(s => s.Id)
                .Take(1)
                .DefaultIfEmpty()

            join w in _wingRepository.GetQueryable() on sdm.WingId equals w.Id into wingj
            from w in wingj.DefaultIfEmpty()

            join pt in _propertyTypeRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id into ptj
            from pt in ptj.DefaultIfEmpty()

            join opm in _propertyOldRepository.GetQueryable() on pm.PropertyMastOldId equals opm.Id into opmj
            from opm in opmj.DefaultIfEmpty()


            where pm.IsActive && !pm.MarkedForDeletion
                  && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
                  && (zoneId == 0 || wm.ZoneId == zoneId)
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)

                  // --- NEW filters applied like PermissionNoticeDataProvider ---
                  && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)
                  && (string.IsNullOrEmpty(type) || pt.Type == type)
                  && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)

            orderby pm.PropertyNo, pm.PartitionNo

            select new
            {
                pm.Id,
                pm.OwnerName,
                pm.Address,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.PlotNo,
                zm.Description,
                wm.WardNo,
                sdm.SocietyName,
                pm.FlatOrShopNo,
                pm.FlatOrShopName,
                pm.MobileNo,
                opm.OldPropertyNo,
                pt.PropertyDescription,
                pm.OccupierName,
                w.WingNo
            };

        var takePlusOne = take == int.MaxValue
    ? int.MaxValue
    : take + 1;

        var orderedBase = propQuery
            .Distinct()
            .OrderBy(x => x.PropertyNo)
            .ThenBy(x => x.PartitionNo);

        var hasFromRange = int.TryParse(fromPropertyNoText, out var fromPropertyNo);
        var hasToRange = int.TryParse(toPropertyNoText, out var toPropertyNo);

        List<int> ownerIdsFromRange = new();

        List<dynamic> fetched;
        bool hasMore;

        if (hasFromRange || hasToRange)
        {
            var queryWithNum = orderedBase.Select(x => new
            {
                Data = x,

                NumericPropertyNo =
                    x.PropertyNo != null
                    && x.PropertyNo.Trim() != ""
                    && !EF.Functions.Like(x.PropertyNo.Trim(), "%[^0-9]%")
                        ? (int?)Convert.ToInt32(x.PropertyNo.Trim())
                        : null
            });

            if (hasFromRange)
            {
                queryWithNum = queryWithNum.Where(x =>
                    x.NumericPropertyNo.HasValue &&
                    x.NumericPropertyNo.Value >= fromPropertyNo);
            }

            if (hasToRange)
            {
                queryWithNum = queryWithNum.Where(x =>
                    x.NumericPropertyNo.HasValue &&
                    x.NumericPropertyNo.Value <= toPropertyNo);
            }

            // IMPORTANT:
            // Get ALL matching OwnerIds before pagination.
            // This gives the report the complete owner-id list for the range,
            // even when the report is paged.
            ownerIdsFromRange = await queryWithNum
                .Select(x => x.Data.Id)
                .Distinct()
                .ToListAsync(ct);

            // Now fetch only the requested page.
            var result = await queryWithNum
                .OrderBy(x => x.NumericPropertyNo)
                .ThenBy(x => x.Data.PartitionNo)
                .Skip(skip)
                .Take(takePlusOne)
                .Select(x => x.Data)
                .ToListAsync(ct);

            fetched = result
                .Cast<dynamic>()
                .ToList();

            hasMore = take != int.MaxValue && fetched.Count > take;

            if (hasMore)
            {
                fetched = fetched.Take(take).ToList();
            }
        }
        else
        {
            var result = await orderedBase
                .Skip(skip)
                .Take(takePlusOne)
                .ToListAsync(ct);

            fetched = result
                .Cast<dynamic>()
                .ToList();

            hasMore = take != int.MaxValue && fetched.Count > take;

            if (hasMore)
            {
                fetched = fetched.Take(take).ToList();
            }
        }

        var props = fetched;

        // ---------------- FINAL ROW ----------------
        var rows = props.Select(p =>
        {
            var row = new Dictionary<string, object?>
            {
                ["OwnerId"] = p.Id,
                ["OwnerName"] = p.OwnerName,
                ["MarathiOwnerAddress"] = p.Address,
                ["PropertyNo"] = p.PropertyNo,
                ["PartitionNo"] = p.PartitionNo,
                ["PlotNo"] = p.PlotNo,

                ["NodeDescription"] = p.Description,
                ["wardNo"] = p.WardNo,
                ["MarathiSocietyName"] = p.SocietyName,
                ["FlatOrShopNo"] = p.FlatOrShopNo,
                ["FlatOrShopName"] = p.FlatOrShopName,
                ["MobileNo"] = p.MobileNo,

                ["OldPropertyNo"] = p.OldPropertyNo,
                ["PropertyDescription"] = p.PropertyDescription,
                ["OccupierName"] = p.OccupierName,
                ["RenterName"] = rm?.RenterName,
                ["RentMonthly"] = rm?.RentMonthly,
                ["WingNo"] = p.WingNo,

                // ---------------- ULB ----------------
                ["CouncilName"] = ulb?.CouncilName,
                ["CouncilAddress"] = ulb?.CouncilAddress,

                ["zoneId"] = " ",
                ["wardId"] = " ",
            };

            return (object)row;
        }).ToList();

        // Attach the COMPLETE matching owner/property ID list to the first row.
        // This list is generated before pagination, so it is not limited
        // to the current report page.
        if (ownerIdsFromRange.Count > 0 &&
            rows.Count > 0 &&
            rows[0] is Dictionary<string, object?> firstRow)
        {
            firstRow["OwnerIdsInRange"] =
                string.Join(",", ownerIdsFromRange.OrderBy(id => id));
        }

        return (rows, hasMore);
    }

}