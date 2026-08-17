using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

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
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _reportRequestRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeMasterRepository;

    public PermissionNoticeDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<WingEntity> wingRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportDataRepository<PropertyTypeMasterEntity> propertyTypeMasterRepository)
    {
        _propertyRepository = propertyRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _wingRepository = wingRepository;
        _yearRepository = yearRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _userRepository = userRepository;
        _reportRequestRepository = reportRequestRepository;
        _transRepository = transRepository;
        _propertyTypeMasterRepository = propertyTypeMasterRepository;
    }

    public IReadOnlyList<ReportSectionDescriptor> GetSections()
    {
        return new[]
        {
            new ReportSectionDescriptor(ImageParamsSection, false),
            new ReportSectionDescriptor(MainSection, false),
            new ReportSectionDescriptor(DetailSection, true)
        };
    }

    public async Task<object> GetDataAsync(
        Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        var (rows, _) = await BuildPageAsync(
            Guid.Empty,
            parameters,
            skip: 0,
            take: int.MaxValue,
            ct);

        return rows;
    }

    public Task<ReportDataPage> GetDataPageAsync(
        Dictionary<string, string> parameters,
        string section,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return GetDataPageAsync(
            Guid.Empty,
            parameters,
            section,
            page,
            pageSize,
            ct);
    }

    public async Task<ReportDataPage> GetDataPageAsync(
        Guid reportRequestId,
        Dictionary<string, string> parameters,
        string section,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 100;
        }

        if (section.Equals(
                ImageParamsSection,
                StringComparison.OrdinalIgnoreCase))
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

        if (section.Equals(
                MainSection,
                StringComparison.OrdinalIgnoreCase)
            || section.Equals(
                DetailSection,
                StringComparison.OrdinalIgnoreCase))
        {
            var skip = (page - 1) * pageSize;

            var (rows, hasMore) = await BuildPageAsync(
                reportRequestId,
                parameters,
                skip,
                pageSize,
                ct);

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

    private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
        Guid reportRequestId,
        Dictionary<string, string> parameters,
        int skip,
        int take,
        CancellationToken ct)
    {
        /*
         * Parse request parameters
         */

        parameters.TryGetValue("ownerId", out var ownerIdText);

        var ownerIds = string.IsNullOrWhiteSpace(ownerIdText)
            ? new List<int>()
            : ownerIdText
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                    int.TryParse(x.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

        int.TryParse(GetParameter(parameters, "zoneId"), out var zoneId);

        int.TryParse(GetParameter(parameters, "wardId"), out var wardId);

        var propertyNoText = GetParameter(parameters, "propertyNo");

        var fromPropertyNoText = GetParameter(parameters, "fromPropertyNo");

        var toPropertyNoText = GetParameter(parameters, "toPropertyNo");

        var partitionNoText = GetParameter(parameters, "partitionNo");

        int.TryParse(GetParameter(parameters, "assessmentStatus"), out var assessmentStatus);

        var type = GetParameter(parameters, "Type")?.ToUpperInvariant();

        int.TryParse(GetParameter(parameters, "propertyTypeId"), out var propertyTypeId);

        var propertyDescription = GetParameter(parameters, "PropertyDescription");

        var financeYear = ParseFinanceYear(parameters);

        /*
         * Resolve the selected financial year only when it is provided.
         */

        var activeYearId = 0;

        if (financeYear != 0)
        {
            activeYearId = await BaseQuery(financeYear)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);
        }

        /*
         * Resolve the user who requested the report.
         */

        var requestedByUserId = await _reportRequestRepository
            .GetQueryable()
            .Where(r =>
                r.ReportRequestId == reportRequestId)
            .Select(r => (int?)r.RequestedByUserId)
            .FirstOrDefaultAsync(ct);

        var user = requestedByUserId.HasValue
            ? await GetUserInfoAsync(
                requestedByUserId.Value,
                ct)
            : null;

        /*
         * Create the base property query.
         */

        var propQuery =
            from property in _propertyRepository.GetQueryable()

            join propertyType in
                _propertyTypeMasterRepository.GetQueryable()
                on property.PropertyTypeId equals propertyType.Id
                into propertyTypeJoin

            from propertyType in propertyTypeJoin.DefaultIfEmpty()

            join ward in _wardRepository.GetQueryable()
                on property.WardId equals ward.Id
                into wardJoin

            from ward in wardJoin.DefaultIfEmpty()

            join zone in _zoneRepository.GetQueryable()
                on ward.ZoneId equals zone.Id
                into zoneJoin

            from zone in zoneJoin.DefaultIfEmpty()

            from society in _societyRepository
                .GetQueryable()
                .Where(s => s.PropertyId == property.Id)
                .OrderBy(s => s.Id)
                .Take(1)
                .DefaultIfEmpty()

            from ulb in _ulbMasterRepository
                .GetQueryable()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Id)
                .Take(1)
                .DefaultIfEmpty()

            where property.IsActive
                  && !property.MarkedForDeletion

                  && (ownerIds.Count == 0 || ownerIds.Contains(property.Id))

                  && (zoneId == 0 || ward.ZoneId == zoneId)

                  && (wardId == 0 || property.WardId == wardId)

                  && (propertyNoText == null || property.PropertyNo == propertyNoText)

                  && (partitionNoText == null || property.PartitionNo == partitionNoText)

                  && (assessmentStatus == 0
                      || property.PropertyAssessmentStatusId == assessmentStatus)

                  && (string.IsNullOrEmpty(type) || propertyType.Type == type)

                  && (propertyTypeId == 0 || propertyType.Id == propertyTypeId)

                  && (string.IsNullOrEmpty(propertyDescription) || propertyType.PropertyDescription == propertyDescription)

            select new
            {
                property.Id,
                property.OwnerName,
                property.Address,
                property.PropertyNo,
                property.PartitionNo,
                property.PlotNo,
                property.Location,
                property.WardId,
                property.FlatOrShopNo,
                property.PropertyAssessmentStatusId,

                WardNo = ward.WardNo,
                ZoneId = ward.ZoneId,
                ZoneDescription = zone.Description,

                SocietyName = society.SocietyName,

                PropertyType = propertyType.Type,
                propertyType.PropertyDescription,

                UlbName = ulb.UlbName,
                UlbAddress = ulb.UlbAddress,
                UlbEmail = ulb.EmailId,
                UlbMobile = ulb.MobileNo,
                UlbState = ulb.State
            };

        /*
         * Apply the finance-year transaction filter only when the
         * caller explicitly supplies a financial year.
         */

        if (financeYear != 0 && activeYearId > 0)
        {
            var transactionPropertyIds = _transRepository
                .GetQueryable()
                .Where(t =>
                    t.FinanceYearId == activeYearId)
                .Select(t => t.PropertyId);

            propQuery = propQuery.Where(property =>
                transactionPropertyIds.Contains(property.Id));
        }

        /*
         * Load all properties matching the main filters.
         *
         * The From/To range is applied after materialization because
         * PropertyNo can contain either numeric or alphanumeric data.
         * This follows WarrentNoticeDataProvider behavior.
         */

        var properties = await propQuery
            .Distinct()
            .OrderBy(x => x.PropertyNo)
            .ThenBy(x => x.PartitionNo)
            .ToListAsync(ct);

        /*
         * Apply inclusive From Property Number.
         */

        if (int.TryParse(
                fromPropertyNoText,
                out var fromPropertyNo))
        {
            properties = properties
                .Where(x =>
                    int.TryParse(
                        x.PropertyNo,
                        out var currentPropertyNo)
                    && currentPropertyNo >= fromPropertyNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(
                     fromPropertyNoText))
        {
            properties = properties
                .Where(x =>
                    string.Compare(
                        x.PropertyNo,
                        fromPropertyNoText,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /*
         * Apply inclusive To Property Number.
         */

        if (int.TryParse(
                toPropertyNoText,
                out var toPropertyNo))
        {
            properties = properties
                .Where(x =>
                    int.TryParse(
                        x.PropertyNo,
                        out var currentPropertyNo)
                    && currentPropertyNo <= toPropertyNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(
                     toPropertyNoText))
        {
            properties = properties
                .Where(x =>
                    string.Compare(
                        x.PropertyNo,
                        toPropertyNoText,
                        StringComparison.OrdinalIgnoreCase) <= 0)
                .ToList();
        }

        /*
         * Apply paging after the From/To range is selected.
         */

        var takePlusOne = take == int.MaxValue
            ? int.MaxValue
            : take + 1;

        var fetched = properties
            .Skip(skip)
            .Take(takePlusOne)
            .ToList();

        var hasMore =
            take != int.MaxValue
            && fetched.Count > take;

        if (hasMore)
        {
            fetched = fetched
                .Take(take)
                .ToList();
        }

        /*
         * Create report rows.
         */

        var rows = fetched
            .Select(property =>
            {
                var row = new Dictionary<string, object?>
                {
                    ["OwnerId"] = property.Id,
                    ["OwnerName"] = property.OwnerName,
                    ["MarathiOwnerAddress"] = property.Address,
                    ["PropertyNo"] = property.PropertyNo,
                    ["PartitionNo"] = property.PartitionNo,
                    ["NodeDescription"] = property.ZoneDescription,
                    ["wardNo"] = property.WardNo,
                    ["MarathiSocietyName"] = property.SocietyName,
                    ["FlatOrShopNo"] = property.FlatOrShopNo,
                    ["CouncilName"] = property.UlbName,
                    ["CouncilAddress"] = property.UlbAddress,
                    ["CouncilEmailId"] = property.UlbEmail,
                    ["CouncilMobileNo"] = property.UlbMobile,
                    ["CouncilState"] = property.UlbState,
                    ["userName"] = user?.UserName,
                    ["zoneId"] = property.ZoneId,
                    ["wardId"] = property.WardId
                };

                return (object)row;
            })
            .ToList();

        return (rows, hasMore);
    }

    private static string? GetParameter(
        IDictionary<string, string> parameters,
        string key)
    {
        if (!parameters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static short ParseFinanceYear(IDictionary<string, string> parameters)
    {
        var financeYearText = GetParameter(
            parameters,
            "financeYear");

        short.TryParse(financeYearText, out var financeYear);

        return financeYear;
    }

    private IQueryable<YearMasterEntity> BaseQuery(short financeYear)
    {
        return _yearRepository
            .GetQueryable()
            .Where(year =>
                financeYear == 0
                    ? year.IsActive
                    : year.Year == financeYear);
    }

    private sealed record UserInfo(int RequestedByUserId, int Id, string UserName);

    private Task<UserInfo?> GetUserInfoAsync(int requestedByUserId, CancellationToken ct)
    {
        return _userRepository
            .GetQueryable()
            .Where(user =>
                user.Id == requestedByUserId)
            .Select(user =>
                new UserInfo(
                    requestedByUserId,
                    user.Id,
                    user.UserName))
            .FirstOrDefaultAsync(ct);
    }
}