using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;

public class KarakarniDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";
    public const string PropertyDetailsSection = "propertyDetails";

    public string ProviderCode => "KarakarniDataProvider";

    private static readonly List<string> ActiveTaxCodes = new()
    {
        "GEN",
        "STATE_EDU",
        "STATE_EMP",
        "TREE",
        "SP_WATER",
        "ROAD",
        "FIRE",
        "LIGHT",
        "WATER_BEN",
        "SEWAGE",
        "SP_EDU"
    };

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<TypeOfUseEntity> _typeOfUseRepository;
    private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
    private readonly IReportDataRepository<FloorEntity> _floorRepository;
    private readonly IReportDataRepository<ConstructionTypeEntity> _constructionTypeRepository;
    private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
    private readonly IReportDataRepository<TaxMasterEntity> _taxMastRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;
    private readonly IReportDataRepository<RVCalculationResultsEntity> _rvResultsRepository;
    private readonly IReportDataRepository<PlotDetailsEntity> _plotDetailsRepository;
    private readonly IReportDataRepository<WingEntity> _wingRepository;

    public KarakarniDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<TypeOfUseEntity> typeOfUseRepository,
        IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
        IReportDataRepository<FloorEntity> floorRepository,
        IReportDataRepository<ConstructionTypeEntity> constructionTypeRepository,
        IReportDataRepository<TransMastEntity> transmastRepository,
        IReportDataRepository<TaxMasterEntity> taxMastRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository,
        IReportDataRepository<RVCalculationResultsEntity> rvResultsRepository,
        IReportDataRepository<PlotDetailsEntity> plotDetailsRepository,
        IReportDataRepository<WingEntity> wingRepository
        )
    {
        _propertyRepository = propertyRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _floorRepository = floorRepository;
        _constructionTypeRepository = constructionTypeRepository;
        _transmastRepository = transmastRepository;
        _taxMastRepository = taxMastRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _userRepository = userRepository;
        _yearRepository = yearRepository;
        _ReportRequestRepository = reportRequestRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _rvResultsRepository = rvResultsRepository;
        _plotDetailsRepository = plotDetailsRepository;
        _wingRepository = wingRepository;
    }

    // Static — never runs a query (avoids any heavy query executing on the authenticate request).
    public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
    {
        new ReportSectionDescriptor(MainSection,            true),
        new ReportSectionDescriptor(PropertyDetailsSection, true),
    };

    public async Task<object> GetDataAsync(
        Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var financeYear = ParseFinanceYear(parameters);
        var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, MainSection, skip: 0, take: int.MaxValue, ct);
        return rows;
    }

    public async Task<ReportDataPage> GetDataPageAsync(
        Guid reportRequestId,
        Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
    {
        var financeYear = ParseFinanceYear(parameters);
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 100;

        var (rows, hasMore) = await BuildPageAsync(reportRequestId, parameters, section, (page - 1) * pageSize, pageSize, ct);
        return new ReportDataPage
        {
            Section = section,
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

    private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, string section, int skip, int take, CancellationToken ct)
    {
        parameters.TryGetValue("ownerId", out var ownerIdText);

        var ownerIds = string.IsNullOrWhiteSpace(ownerIdText) ? new List<int>() : ownerIdText
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

        // --- Parse parameters ---
        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        parameters.TryGetValue("assessmentStatus", out var assessmentStatusText);
        int.TryParse(assessmentStatusText, out var assessmentStatus);

        // ------ FROM Property - TO Property Number Range Filter Parameters ------
        parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
        fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText)
            ? null
            : fromPropertyNoText.Trim();

        parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
        toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText)
            ? null
            : toPropertyNoText.Trim();

        // propertyId accepts a single value OR comma-separated list: "101,202,303" propertyid means owenerid
        parameters.TryGetValue("propertyId", out var propertyIdStr);
        parameters.TryGetValue("userId", out var userIdStr);

        parameters.TryGetValue("Type", out var type);
        type = string.IsNullOrWhiteSpace(type)
            ? null
            : type.Trim().ToUpper();

        parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
        int.TryParse(propertyTypeIdText, out var propertyTypeId);

        parameters.TryGetValue("PropertyDescription", out var propertyDescription);
        propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();

        var financeYear = ParseFinanceYear(parameters);
        var activeYear = await BaseQuery(financeYear).FirstOrDefaultAsync(ct);
        int activeYearId = activeYear?.Id ?? 0;

        // Split on commas, parse each token, deduplicate, drop invalid entries.
        var propertyIds = (propertyIdStr ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ownerIds.Count > 0)
        {
            propertyIds.AddRange(ownerIds);
            propertyIds = propertyIds.Distinct().ToList();
        }

        // If no explicit propertyIds are provided, resolve them via filters
        if (propertyIds.Count == 0)
        {
            var query =
                from p in _propertyRepository.GetQueryable()
                join w in _wardRepository.GetQueryable() on p.WardId equals w.Id into wj
                from w in wj.DefaultIfEmpty()

                    // ---------------- JOIN PropertyTypeMaster ----------------
                join pt in _propertyTypeRepository.GetQueryable()
                    on p.PropertyTypeId equals pt.Id into ptj
                from pt in ptj.DefaultIfEmpty()

                where p.IsActive && !p.MarkedForDeletion
                      && (zoneId == 0 || w.ZoneId == zoneId)
                      && (wardId == 0 || p.WardId == wardId)
                      && (propertyNoText == null || p.PropertyNo == propertyNoText)
                      && (partitionNoText == null || p.PartitionNo == partitionNoText)
                      && (assessmentStatus == 0 || p.PropertyAssessmentStatusId == assessmentStatus)
                      && (string.IsNullOrEmpty(type) || p.Type == type)
                      && (propertyTypeId == 0 || p.PropertyTypeId == propertyTypeId)
                      && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)
                      && (fromPropertyNoText == null || string.Compare(p.PropertyNo, fromPropertyNoText) >= 0)
                      && (toPropertyNoText == null || string.Compare(p.PropertyNo, toPropertyNoText) <= 0)

                select p;

            // APPLY TransMast constraint SERVER-SIDE only when caller requested financeYear
            if (financeYear != 0 && activeYearId > 0)
            {
                var transQ = _transmastRepository.GetQueryable()
                    .Where(t => t.FinanceYearId == activeYearId && t.IsActive && !t.MarkedForDeletion)
                    .Select(t => t.PropertyId);

                query = query.Where(p => transQ.Contains(p.Id));
            }

            propertyIds = await query.Select(p => p.Id).ToListAsync(ct);
        }

        int.TryParse(userIdStr, out var userId);

        // Paginate property IDs first to prevent child/details mixing across pages
        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
        var pagedPropertyIds = propertyIds.Skip(skip).Take(takePlusOne).ToList();
        var hasMore = take != int.MaxValue && pagedPropertyIds.Count > take;
        if (hasMore) pagedPropertyIds = pagedPropertyIds.Take(take).ToList();

        List<object> pagedRows;

        switch (section)
        {
            case PropertyDetailsSection:
                pagedRows = await BuildPropertyDetailsRowsAsync(pagedPropertyIds, ct);
                break;

            default: // MainSection
                pagedRows = await BuildMainRowsAsync(pagedPropertyIds, reportRequestId, activeYearId, activeYear?.YearCode ?? "", ct);
                break;
        }

        return (pagedRows, hasMore);
    }

    private async Task<List<object>> BuildMainRowsAsync(List<int> propertyIds, Guid reportRequestId, int activeYearId, string yearCode, CancellationToken ct)
    {
        if (propertyIds == null || propertyIds.Count == 0)
            return new List<object>();

        // 1a. All properties + Ward (LEFT JOIN) in one batch
        var properties = await (
            from pm in _propertyRepository.GetQueryable()
                                           .Where(p => propertyIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
            join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wj
            from wm in wj.DefaultIfEmpty()
            join pt in _propertyTypeRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id into ptj
            from pt in ptj.DefaultIfEmpty()
            select new
            {
                pm.Id,
                pm.PropertyNo,
                pm.WardId,
                pm.PartitionNo,
                pm.UPICId,
                pm.SubZoneNo,
                pm.MobileNo,
                pm.OwnerTitle,
                pm.OccupierTitle,
                pm.OwnerName,
                pm.OccupierName,
                pm.Address,
                pm.PlotNo,
                pm.FlatOrShopNo,
                pm.FlatOrShopName,
                pm.PropertyTypeId,
                PropertyDescription = pt != null ? pt.PropertyDescription : null,
                WardNo = wm != null ? wm.WardNo : null,
                ZoneNo = wm != null && wm.Zone != null ? wm.Zone.ZoneNo : null,
            }
        ).ToListAsync(ct);

        if (!properties.Any())
            return new List<object>();

        // 1b. Society details (batch load for all property IDs)
        var societyDetails = await _societyRepository.GetQueryable()
            .Where(sd => sd.PropertyId.HasValue && propertyIds.Contains(sd.PropertyId.Value))
            .Select(sd => new
            {
                PropertyId = sd.PropertyId!.Value,
                sd.WingId,
                sd.WingName,
                sd.SocietyName,
                sd.SocietyAddress,
            })
            .ToListAsync(ct);

        var wingIds = societyDetails
            .Where(sd => sd.WingId.HasValue)
            .Select(sd => sd.WingId!.Value)
            .Distinct()
            .ToList();

        var wingMap = wingIds.Any()
            ? await _wingRepository.GetQueryable()
                .Where(w => wingIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WingNo, ct)
            : new Dictionary<int, string>();

        var societyMap = societyDetails
            .GroupBy(sd => sd.PropertyId)
            .ToDictionary(g => g.Key, g => g.First());

        // 1c. Type-of-use (batch load for unique PropertyTypeIds)
        var uniqueTypeOfUseIds = properties
            .Where(p => p.PropertyTypeId.HasValue)
            .Select(p => p.PropertyTypeId!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, dynamic> typeOfUseMap;
        if (uniqueTypeOfUseIds.Any())
        {
            var typeOfUseData = await _typeOfUseRepository.GetQueryable()
                .Where(t => uniqueTypeOfUseIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    t.Description,
                    t.TypeOfUseCode,
                })
                .ToListAsync(ct);
            typeOfUseMap = typeOfUseData.ToDictionary(t => t.Id, t => (dynamic)t);
        }
        else
        {
            typeOfUseMap = new Dictionary<int, dynamic>();
        }

        // 1d. ULB Master — SELECT from [CORE].[UlbMaster] (first/only row)
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

        // 1e. User: select from [CORE].[UserMaster] where Id = @userId
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
                    u.UserCode,
                    u.Email,
                    u.MobileNo,
                })
                .FirstOrDefaultAsync(ct);

        // 1f. TransMast pivot — fetch all tax lines for all properties, then group by PropertyId
        var taxRowsAll = await (
            from tm in _transmastRepository.GetQueryable()
                .Where(t => propertyIds.Contains(t.PropertyId) && (activeYearId == 0 || t.FinanceYearId == activeYearId) && t.IsActive && !t.MarkedForDeletion)
            join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
            orderby tam.DisplayOrder
            select new
            {
                tm.PropertyId,
                tm.Id,
                tam.TaxCode,
                tam.TaxName,
                RVorCV = tm.CalculationType,
                RVorCVValue = tm.CalculationValue,
                CalculationAnnualValue = tm.CalculationAnnualValue,
                tm.TaxAmount,
            }
        ).ToListAsync(ct);

        var taxRowsByProperty = taxRowsAll.GroupBy(t => t.PropertyId)
                                          .ToDictionary(g => g.Key, g => g.ToList());

        // No custom allSafeTaxCodes lookup list needed, as we strictly output only the 11 active tax codes.

        // RV Calculation results for aggregates (varshik bhade mulya & durusti)
        var rvResults = await _rvResultsRepository.GetQueryable()
                 .Where(r => propertyIds.Contains(r.PropertyId) && r.IsActive && !r.MarkedForDeletion)
            .Select(r => new
            {
                r.PropertyId,
                r.AnnualRentalValue,
                r.Maintenance,
                r.RateableValue
            })
            .ToListAsync(ct);

        var rvResultsMap = rvResults.GroupBy(r => r.PropertyId)
                                    .ToDictionary(g => g.Key, g => g.ToList());

        // Fetch total built-up area in Sq. Mtr. for each property
        var floorAreas = await _propertyDetailsRepository.GetQueryable()
            .Where(pd => propertyIds.Contains(pd.PropertyId) && pd.IsActive && !pd.MarkedForDeletion)
            .GroupBy(pd => pd.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                TotalAreaSqMtr = g.Sum(x => x.BuiltupAreaSqMeter ?? 0)
            })
            .ToDictionaryAsync(x => x.PropertyId, x => (decimal)x.TotalAreaSqMtr, ct);

        // Fetch open plot area in Sq. Ft. for each property
        var plotAreas = await _plotDetailsRepository.GetQueryable()
            .Where(pd => pd.PropertyId.HasValue && propertyIds.Contains(pd.PropertyId.Value) && !pd.MarkedForDeletion)
            .GroupBy(pd => pd.PropertyId!.Value)
            .Select(g => new
            {
                PropertyId = g.Key,
                TotalPlotAreaSqFt = g.Sum(x => x.PlotArea ?? 0)
            })
            .ToDictionaryAsync(x => x.PropertyId, x => (decimal)x.TotalPlotAreaSqFt, ct);

        // Stable sort properties to match original propertyIds order
        var propertyMap = properties.ToDictionary(p => p.Id);
        var orderedProperties = propertyIds
            .Where(id => propertyMap.ContainsKey(id))
            .Select(id => propertyMap[id])
            .ToList();

        // Build one row per property
        var allRows = new List<object>();

        foreach (var property in orderedProperties)
        {
            var society = societyMap.ContainsKey(property.Id) ? societyMap[property.Id] : null;
            var typeOfUse = property.PropertyTypeId.HasValue && typeOfUseMap.ContainsKey(property.PropertyTypeId.Value)
                ? typeOfUseMap[property.PropertyTypeId.Value]
                : null;

            var taxRows = taxRowsByProperty.TryGetValue(property.Id, out var propTaxes)
                ? propTaxes.Cast<dynamic>().ToList()
                : new List<dynamic>();

            rvResultsMap.TryGetValue(property.Id, out var propRvResults);

            // Determine active calculation type (RV or CV)
            var hasRv = taxRows.Any(t => string.Equals((string)t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase));
            var hasCv = taxRows.Any(t => string.Equals((string)t.RVorCV, "CV", StringComparison.OrdinalIgnoreCase));

            if (hasCv && !hasRv)
            {
                continue;
            }

            string activeCalcType = "RV";
            if (hasRv && hasCv)
            {
                activeCalcType = "RCV";
            }
            else if (hasRv)
            {
                taxRows = taxRows.Where(t => string.Equals((string)t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase)).ToList();
                activeCalcType = "RV";
            }

            double totalAnnualRentalValue = 0;
            decimal totalMaintenance = 0;
            decimal totalRateableValue = 0;

            if ((string.Equals(activeCalcType, "RV", StringComparison.OrdinalIgnoreCase) || string.Equals(activeCalcType, "RCV", StringComparison.OrdinalIgnoreCase)) && propRvResults != null && propRvResults.Any())
            {
                totalAnnualRentalValue = propRvResults.Sum(r => (double)(r.AnnualRentalValue ?? 0d));
                totalMaintenance = propRvResults.Sum(r => (decimal)(r.Maintenance ?? 0m));
                totalRateableValue = propRvResults.Sum(r => (decimal)(r.RateableValue ?? 0m));
            }

            var taxTotalRows = taxRows.Where(t => t.TaxCode == "TaxTotal").ToList();
            decimal totalTax = 0;
            if (string.Equals(activeCalcType, "RCV", StringComparison.OrdinalIgnoreCase))
            {
                if (taxTotalRows.Any())
                {
                    totalTax = taxTotalRows.Sum(t => (decimal)t.TaxAmount);
                }
                else
                {
                    totalTax = taxRows.Where(t => t.TaxCode != "TaxTotal").Sum(t => (decimal)t.TaxAmount);
                }
            }
            else
            {
                var taxTotalRow = taxTotalRows.FirstOrDefault(t => string.Equals((string)t.RVorCV, activeCalcType, StringComparison.OrdinalIgnoreCase));
                totalTax = taxTotalRow != null
                    ? taxTotalRow.TaxAmount
                    : taxRows.Where(t => t.TaxCode != "TaxTotal" && string.Equals((string)t.RVorCV, activeCalcType, StringComparison.OrdinalIgnoreCase)).Sum(t => (decimal)t.TaxAmount);
            }

            // Fallback to transaction values if RV results are missing or zero
            if ((string.Equals(activeCalcType, "RV", StringComparison.OrdinalIgnoreCase) || string.Equals(activeCalcType, "RCV", StringComparison.OrdinalIgnoreCase)) && totalAnnualRentalValue == 0)
            {
                var firstTaxTotal = taxTotalRows.FirstOrDefault(t => string.Equals((string)t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase));
                if (firstTaxTotal != null)
                {
                    totalAnnualRentalValue = (double)(firstTaxTotal.CalculationAnnualValue ?? 0m);
                }
            }
            if ((string.Equals(activeCalcType, "RV", StringComparison.OrdinalIgnoreCase) || string.Equals(activeCalcType, "RCV", StringComparison.OrdinalIgnoreCase)) && totalRateableValue == 0)
            {
                var firstTaxTotal = taxTotalRows.FirstOrDefault(t => string.Equals((string)t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase));
                if (firstTaxTotal != null)
                {
                    totalRateableValue = firstTaxTotal.RVorCVValue;
                }
            }

            floorAreas.TryGetValue(property.Id, out var totalAreaSqMtr);
            plotAreas.TryGetValue(property.Id, out var openPlotAreaSqFt);

            var row = new Dictionary<string, object?>
            {
                ["propertyId"] = property.Id,
                ["PropertyId"] = property.Id,
                ["totalAreaSqMtr"] = totalAreaSqMtr,
                ["openPlotAreaSqFt"] = openPlotAreaSqFt,
                ["propertyNo"] = property.PropertyNo,
                ["zoneNo"] = property.ZoneNo,
                ["wardId"] = property.WardId,
                ["wardNo"] = property.WardNo,
                ["partitionNo"] = property.PartitionNo,
                ["upicId"] = property.UPICId,
                ["subZoneNo"] = property.SubZoneNo,
                ["mobileNo"] = property.MobileNo,
                ["ownerTitle"] = property.OwnerTitle,
                ["occupierTitle"] = property.OccupierTitle,
                ["ownerName"] = property.OwnerName,
                ["occupierName"] = property.OccupierName,
                ["address"] = property.Address,
                ["plotNo"] = property.PlotNo,
                ["flatOrShopNo"] = property.FlatOrShopNo,
                ["flatOrShopName"] = property.FlatOrShopName,
                // Society details
                ["wingNo"] = (society?.WingId != null && wingMap.TryGetValue(society.WingId.Value, out var wNo)) ? wNo : null,
                ["wingName"] = society?.WingName,
                ["societyName"] = society?.SocietyName,
                ["societyAddress"] = society?.SocietyAddress,
                // Type-of-use
                ["typeOfUseDesc"] = typeOfUse?.Description,
                ["Description"] = property.PropertyDescription,
                ["typeOfUseCode"] = typeOfUse?.TypeOfUseCode,
                // User Master fields (CORE.UserMaster)
                ["userId"] = user?.Id,
                ["userName"] = user?.UserName,
                ["userCode"] = user?.UserCode,
                ["userEmail"] = user?.Email,
                ["userMobileNo"] = user?.MobileNo,
                // ULB Master fields (CORE.UlbMaster)
                ["ulbCode"] = ulb?.UlbCode,
                ["ulbName"] = ulb?.UlbName,
                ["ulbNameLocal"] = ulb?.UlbNameLocal,
                ["ulbLogo"] = ulb?.UlbLogo,
                ["ulbEmailId"] = ulb?.EmailId,
                ["emailId"] = ulb?.EmailId,
                ["ulbMobileNo"] = ulb?.MobileNo,
                ["mobileNo"] = ulb?.MobileNo,
                ["ulbAlternateMobileNo"] = ulb?.AlternateMobileNo,
                ["alternateMobileNo"] = ulb?.AlternateMobileNo,
                ["ulbWebsiteUrl"] = ulb?.WebsiteUrl,
                ["websiteUrl"] = ulb?.WebsiteUrl,
                ["ulbAddress"] = ulb?.UlbAddress,
                ["ulbState"] = ulb?.State,
                ["state"] = ulb?.State,
                ["ulbDistrict"] = ulb?.District,
                ["district"] = ulb?.District,
                ["ulbPinCode"] = ulb?.PinCode,
                ["pinCode"] = ulb?.PinCode,
                ["financeYear"] = yearCode,

                // RV Calculation properties (ADDED)
                ["calculationAnnualValue"] = totalAnnualRentalValue,
                ["CalculationAnnualValue"] = totalAnnualRentalValue,
                ["annualRentalValue"] = totalAnnualRentalValue,
                ["AnnualRentalValue"] = totalAnnualRentalValue,
                ["maintenance"] = totalMaintenance,
                ["Maintenance"] = totalMaintenance,
                ["rateableValue"] = totalRateableValue,
                ["RateableValue"] = totalRateableValue,
                ["totalTax"] = totalTax,
                ["TotalTax"] = totalTax,
                ["ekunKar"] = totalTax,
            };

            // Pre-populate only the 11 active tax codes with default 0/activeCalcType value
            foreach (var code in ActiveTaxCodes)
            {
                row[$"Transmast_{code}"] = 0m;
                row[$"RVorCV_{code}"] = activeCalcType;
                row[$"RVorCVValue_{code}"] = 0m;
            }

            // Pivot TransMast rows into dynamic columns on the same main row.
            var componentTaxes = taxRows
                .Where(t => t.TaxCode != "TaxTotal")
                .ToList();
            foreach (var tax in componentTaxes)
            {
                var safeCode = GetSafeTaxCode((string?)tax.TaxCode, (string?)tax.TaxName);
                if (ActiveTaxCodes.Contains(safeCode))
                {
                    row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                    row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                    row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
                }
            }

            allRows.Add(row);
        }

        return allRows;
    }

    private async Task<List<object>> BuildPropertyDetailsRowsAsync(List<int> propertyIds, CancellationToken ct)
    {
        if (propertyIds == null || propertyIds.Count == 0)
            return new List<object>();

        var details = await (
            from pd in _propertyDetailsRepository.GetQueryable()
                     .Where(p => propertyIds.Contains(p.PropertyId) && p.IsActive && !p.MarkedForDeletion)
            join fm in _floorRepository.GetQueryable()
                     on pd.FloorId equals fm.Id into fmj
            from fm in fmj.DefaultIfEmpty()
            join ctm in _constructionTypeRepository.GetQueryable()
                     on pd.ConstructionTypeId equals ctm.Id into ctmj
            from ctm in ctmj.DefaultIfEmpty()
            join tum in _typeOfUseRepository.GetQueryable()
                     on pd.TypeOfUseId equals tum.Id into tumj
            from tum in tumj.DefaultIfEmpty()
            join rv in _rvResultsRepository.GetQueryable().Where(r => r.IsActive && !r.MarkedForDeletion)
                     on pd.Id equals rv.PropertyDetailsId into rvj
            from rv in rvj.DefaultIfEmpty()
            select new
            {
                pd.PropertyId,
                pd.FloorId,
                pd.SubFloorId,
                FloorDescription = fm != null ? fm.Description : null,
                pd.ConstructionYear,
                ConstructionCode = ctm != null ? ctm.ConstructionCode : null,
                ConstructionDescription = ctm != null ? ctm.Description : null,
                TypeOfUseCode = tum != null ? tum.TypeOfUseCode : null,
                TypeOfUseDescription = tum != null ? tum.Description : null,
                TypeOfUseType = tum != null ? tum.Type : null,
                pd.AssessmentYear,
                pd.CarpetAreaSqFeet,
                pd.CarpetAreaSqMeter,
                pd.BuiltupAreaSqFeet,
                pd.BuiltupAreaSqMeter,
                pd.NoOfRooms,
                MonthlyRate = rv != null ? (decimal?)Convert.ToDecimal(rv.MonthlyRate) : 0m,
                YearlyRate = rv != null ? (decimal?)Convert.ToDecimal(rv.YearlyRate) : 0m,
                YearlyRent = rv != null ? (decimal?)Convert.ToDecimal(rv.YearlyRent) : 0m,
                Depreciation = rv != null ? rv.Depreciation : 0m,
                DepreciationPer = rv != null ? rv.DepreciationPer : 0m,
                AnnualRentalValue = rv != null ? (decimal?)Convert.ToDecimal(rv.AnnualRentalValue) : 0m,
                Maintenance = rv != null ? rv.Maintenance : 0m,
                RateableValue = rv != null ? rv.RateableValue : 0m,
            }
        ).ToListAsync(ct);

        // Group by PropertyId and calculate sum of AnnualRentalValue, Maintenance, and RateableValue
        var propertyTotals = details.GroupBy(d => d.PropertyId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalAnnualRentalValue = g.Sum(x => x.AnnualRentalValue ?? 0m),
                TotalMaintenance = g.Sum(x => x.Maintenance ?? 0m),
                TotalRateableValue = g.Sum(x => x.RateableValue ?? 0m)
            });

        // Sort details by the requested propertyIds order, then by floor/subfloor
        var propertyOrderMap = propertyIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
        var orderedDetails = details
            .OrderBy(d => propertyOrderMap.ContainsKey(d.PropertyId) ? propertyOrderMap[d.PropertyId] : int.MaxValue)
            .ThenBy(d => d.FloorId)
            .ThenBy(d => d.SubFloorId)
            .ToList();

        return orderedDetails.Select(pd =>
        {
            propertyTotals.TryGetValue(pd.PropertyId, out var totals);
            return (object)new Dictionary<string, object?>
            {
                ["propertyId"] = pd.PropertyId,
                ["PropertyId"] = pd.PropertyId,
                ["floorId"] = pd.FloorId,
                ["subFloorId"] = pd.SubFloorId,
                ["floorDescription"] = pd.FloorDescription,
                ["constructionYear"] = pd.ConstructionYear,
                ["constructionCode"] = pd.ConstructionCode,
                ["constructionDescription"] = pd.ConstructionDescription,
                ["typeOfUseCode"] = pd.TypeOfUseCode,
                ["typeOfUseDescription"] = pd.TypeOfUseDescription,
                ["typeOfUseType"] = pd.TypeOfUseType,
                ["assessmentYear"] = pd.AssessmentYear,
                ["carpetAreaSqFeet"] = pd.CarpetAreaSqFeet,
                ["carpetAreaSqMeter"] = pd.CarpetAreaSqMeter,
                ["builtupAreaSqFeet"] = pd.BuiltupAreaSqFeet,
                ["builtupAreaSqMeter"] = pd.BuiltupAreaSqMeter,
                ["noOfRooms"] = pd.NoOfRooms,
                ["monthlyRate"] = pd.MonthlyRate,
                ["yearlyRate"] = pd.YearlyRate,
                ["yearlyRent"] = pd.YearlyRent,
                ["depreciation"] = pd.Depreciation,
                ["depreciationPer"] = pd.DepreciationPer,
                ["annualRentalValue"] = pd.AnnualRentalValue,
                ["AnnualRentalValue"] = pd.AnnualRentalValue,
                ["maintenance"] = pd.Maintenance,
                ["Maintenance"] = pd.Maintenance,
                ["rateableValue"] = pd.RateableValue,
                ["RateableValue"] = pd.RateableValue,
                ["totalAnnualRentalValue"] = totals?.TotalAnnualRentalValue ?? 0m,
                ["TotalAnnualRentalValue"] = totals?.TotalAnnualRentalValue ?? 0m,
                ["totalMaintenance"] = totals?.TotalMaintenance ?? 0m,
                ["TotalMaintenance"] = totals?.TotalMaintenance ?? 0m,
                ["totalRateableValue"] = totals?.TotalRateableValue ?? 0m,
                ["TotalRateableValue"] = totals?.TotalRateableValue ?? 0m,
            };
        }).ToList();
    }

    private async Task<List<object>> BuildTaxDetailsRowsAsync(List<int> propertyIds, int activeYearId, CancellationToken ct)
    {
        if (propertyIds == null || propertyIds.Count == 0)
            return new List<object>();

        var taxRows = await (
            from tm in _transmastRepository.GetQueryable()
                .Where(t => propertyIds.Contains(t.PropertyId) && (activeYearId == 0 || t.FinanceYearId == activeYearId) && t.IsActive && !t.MarkedForDeletion)
            join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
            orderby tam.DisplayOrder
            select new
            {
                tm.PropertyId,
                tam.TaxCode,
                tam.TaxName,
                tam.DisplayOrder,
                RVorCV = tm.CalculationType,
                RVorCVValue = tm.CalculationValue,
                tm.TaxAmount,
            }
        ).ToListAsync(ct);

        if (!taxRows.Any())
            return new List<object>();

        // Group by PropertyId and pivot each property's tax lines into a separate row
        var result = new List<object>();
        var taxRowsByProperty = taxRows.GroupBy(t => t.PropertyId);

        foreach (var propertyTaxGroup in taxRowsByProperty)
        {
            // Pivot all tax lines into a single row with dynamic column names.
            var row = new Dictionary<string, object?>
            {
                ["propertyId"] = propertyTaxGroup.Key
            };

            // Pre-populate all active taxes with default 0m/RV values
            foreach (var code in ActiveTaxCodes)
            {
                row[$"Transmast_{code}"] = 0m;
                row[$"RVorCV_{code}"] = "RV";
                row[$"RVorCVValue_{code}"] = 0m;
            }

            var componentTaxes = propertyTaxGroup.Where(t => t.TaxCode != "TaxTotal");
            foreach (var tax in componentTaxes)
            {
                var safeCode = GetSafeTaxCode((string?)tax.TaxCode, (string?)tax.TaxName);
                if (ActiveTaxCodes.Contains(safeCode))
                {
                    row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                    row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                    row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
                }
            }

            result.Add(row);
        }

        return result;
    }

    private async Task<List<object>> BuildFloorDetailsRowsAsync(List<int> propertyIds, CancellationToken ct)
    {
        if (propertyIds == null || propertyIds.Count == 0)
            return new List<object>();

        var rows = await (
            from pd in _propertyDetailsRepository.GetQueryable()
                                                  .Where(p => propertyIds.Contains(p.PropertyId) && p.IsActive && !p.MarkedForDeletion)
            join fm in _floorRepository.GetQueryable()
                     on pd.FloorId equals fm.Id into fmj
            from fm in fmj.DefaultIfEmpty()
            join ctm in _constructionTypeRepository.GetQueryable()
                     on pd.ConstructionTypeId equals ctm.Id into ctmj
            from ctm in ctmj.DefaultIfEmpty()
            join tum in _typeOfUseRepository.GetQueryable()
                     on pd.TypeOfUseId equals tum.Id into tumj
            from tum in tumj.DefaultIfEmpty()
            select new
            {
                pd.PropertyId,
                FloorDescription = fm != null ? fm.Description : null,
                pd.ConstructionYear,
                ConstructionCode = ctm != null ? ctm.ConstructionCode : null,
                ConstructionDescription = ctm != null ? ctm.Description : null,
                TypeOfUseCode = tum != null ? tum.TypeOfUseCode : null,
                TypeOfUseDescription = tum != null ? tum.Description : null,
                TypeOfUseType = tum != null ? tum.Type : null,
                pd.CarpetAreaSqMeter,
                pd.CarpetAreaSqFeet,
                pd.BuiltupAreaSqMeter,
                pd.BuiltupAreaSqFeet,
                pd.NoOfRooms,
            }
        ).ToListAsync(ct);

        return rows.Select(r => (object)new Dictionary<string, object?>
        {
            ["propertyId"] = r.PropertyId,
            ["floorDescription"] = r.FloorDescription,
            ["constructionYear"] = r.ConstructionYear,
            ["constructionCode"] = r.ConstructionCode,
            ["constructionDescription"] = r.ConstructionDescription,
            ["typeOfUseCode"] = r.TypeOfUseCode,
            ["typeOfUseDescription"] = r.TypeOfUseDescription,
            ["typeOfUseType"] = r.TypeOfUseType,
            ["carpetAreaSqMeter"] = r.CarpetAreaSqMeter,
            ["carpetAreaSqFeet"] = r.CarpetAreaSqFeet,
            ["builtupAreaSqMeter"] = r.BuiltupAreaSqMeter,
            ["builtupAreaSqFeet"] = r.BuiltupAreaSqFeet,
            ["noOfRooms"] = r.NoOfRooms,
        }).ToList();
    }

    private static string GetSafeTaxCode(string? taxCode, string? taxName)
    {
        var rawCode = !string.IsNullOrWhiteSpace(taxCode) ? taxCode : taxName ?? "UNKNOWN";
        return rawCode.Replace("SP_E DU", "SP_EDU").Replace(' ', '_');
    }
}
