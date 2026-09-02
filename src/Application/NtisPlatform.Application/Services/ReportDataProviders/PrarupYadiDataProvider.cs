using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders;
/// <summary>
/// NoticeNew report data — one pivoted row per property with dynamic Transmast_{TaxCode} /
/// TaxPending_{TaxCode} columns. Reads from the read-only replica and paginates BY PROPERTY:
/// each page selects a bounded set of properties, fetches only those properties' tax rows, and
/// pivots them in memory — so a large ward never materializes the full cartesian result at once.
/// Section discovery is static (no query runs during authenticate).
/// </summary>
public class PrarupYadiDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "PrarupYadi";
    public const string DetailSection = "PrarupYadiTaxInfo";
    public const string ImageParamsSection = "_reportImageParams";
    public string ProviderCode => "PrarupYadiDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<PropertyImagesMastEntity> _propertyImagesRepository;
    private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportDataRepository<TaxMasterEntity> _taxRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _PropertyTypeMasterRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportDataRepository<FloorEntity> _floorRepository;
    private readonly IReportDataRepository<ConstructionTypeEntity> _constructionTypeRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyDetailsRepository;
    private readonly IReportDataRepository<RVCalculationResultsEntity> _rvCalculationResultsRepository;
    private readonly IReportDataRepository<TypeOfUseEntity> _typeOfUseRepository;
    private readonly IReportDataRepository<RenterMastEntity> _renterMastRepository;

    public PrarupYadiDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<PropertyImagesMastEntity> propertyImagesRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportDataRepository<TaxMasterEntity> taxRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<PropertyTypeMasterEntity> PropertyTypeMasterRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportDataRepository<FloorEntity> floorRepository,
        IReportDataRepository<ConstructionTypeEntity> constructionTypeRepository,
        IReportDataRepository<SocietyDetailsEntity> societyDetailsRepository,
        IReportDataRepository<RVCalculationResultsEntity> rvCalculationResultsRepository,
        IReportDataRepository<TypeOfUseEntity> typeOfUseRepository,
        IReportDataRepository<RenterMastEntity> renterMastRepository)
    {
        _propertyRepository = propertyRepository;
        _propertyImagesRepository = propertyImagesRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _transRepository = transRepository;
        _taxRepository = taxRepository;
        _yearRepository = yearRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _PropertyTypeMasterRepository = PropertyTypeMasterRepository;
        _ReportRequestRepository = reportRequestRepository;
        _userRepository = userRepository;
        _floorRepository = floorRepository;
        _constructionTypeRepository = constructionTypeRepository;
        _societyDetailsRepository = societyDetailsRepository;
        _rvCalculationResultsRepository = rvCalculationResultsRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _renterMastRepository = renterMastRepository;
    }

    public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
    {
        new ReportSectionDescriptor(ImageParamsSection, false),
        new ReportSectionDescriptor(MainSection, false),
        new ReportSectionDescriptor(DetailSection, true),
    };

    public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        var financeYear = ParseFinanceYear(parameters);
        var mainRows = await MainQuery(financeYear, Guid.Empty, parameters, 0, int.MaxValue, ct);
        var (detailRows, _) = await DetailQuery(Guid.Empty, parameters, 0, int.MaxValue, ct);
        return new { main = mainRows, PrarupYadi = detailRows };
    }

    public async Task<ReportDataPage> GetDataPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
    {
        var financeYear = ParseFinanceYear(parameters);

        if (section.Equals(ImageParamsSection, StringComparison.OrdinalIgnoreCase))
        {
            return new ReportDataPage
            {
                Section = ImageParamsSection,
                Page = 1,
                PageSize = 1,
                TotalCount = 1,
                HasMore = false,
                Rows = new List<object>(),
            };
        }

        if (section.Equals(MainSection, StringComparison.OrdinalIgnoreCase))
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = int.MaxValue;

            var skip = pageSize == int.MaxValue
                ? 0
                : (page - 1) * pageSize;

            var rows = await MainQuery(
                financeYear,
                reportRequestId,
                parameters,
                skip,
                pageSize,
                ct);

            return new ReportDataPage
            {
                Section = MainSection,
                Page = page,
                PageSize = rows.Count,
                TotalCount = rows.Count,
                HasMore = false,
                Rows = rows.Cast<object>().ToList(),
            };
        }

        if (section.Equals(DetailSection, StringComparison.OrdinalIgnoreCase))
        {
            if (page < 1)
                page = 1;

            var skip = (page - 1) * pageSize;

            var (rows, hasMore) = await DetailQuery(
                reportRequestId,
                parameters,
                skip,
                pageSize,
                ct);

            return new ReportDataPage
            {
                Section = DetailSection,
                Page = page,
                PageSize = pageSize,
                TotalCount = -1,
                Rows = rows,
                HasMore = hasMore
            };
        }

        return new ReportDataPage
        {
            Section = section,
            Page = page,
            PageSize = pageSize,
            HasMore = false
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


    // ------------------- MAIN SECTION REPORT FIELDS -----------------
    private async Task<List<object>> MainQuery(short financeYear, Guid reportRequestId, Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        parameters.TryGetValue("ownerId", out var ownerIdText);

        var ownerIds = string.IsNullOrWhiteSpace(ownerIdText)
            ? new List<int>()
            : ownerIdText
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
        type = string.IsNullOrWhiteSpace(type)
            ? null
            : type.Trim().ToUpper();

        parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
        int.TryParse(propertyTypeIdText, out var propertyTypeId);

        parameters.TryGetValue("PropertyDescription", out var propertyDescription);
        propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();

        // ------- Amount Parameter ---------
        parameters.TryGetValue("totalTaxFilterType", out var totalTaxFilterType);   // Top n / Less Than / Greater Than
        parameters.TryGetValue("totalTaxFilterValue", out var totalTaxFilterValue); // amount 

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
        var properties =
        (
            from pm in _propertyRepository.GetQueryable()

            join wn in _wardRepository.GetQueryable() on pm.WardId equals wn.Id into wmj
            from wn in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

            join sdm in _societyRepository.GetQueryable() on pm.Id equals sdm.PropertyId into sdmj
            from sdm in sdmj.DefaultIfEmpty()

            join pt in _PropertyTypeMasterRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id into ptj
            from pt in ptj.DefaultIfEmpty()

            from ulb in _ulbMasterRepository.GetQueryable()
                .Where(x => x.IsActive)
                .Take(1)

            where pm.IsActive
      && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
      && (zoneId == 0 || wn.ZoneId == zoneId)
      && (wardId == 0 || pm.WardId == wardId)
      && (propertyNoText == null || pm.PropertyNo == propertyNoText)
      && (partitionNoText == null || pm.PartitionNo == partitionNoText)
      && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)
      && (string.IsNullOrEmpty(type) || pt.Type == type)
      && (propertyTypeId == 0 || pt.Id == propertyTypeId)
      && (string.IsNullOrEmpty(propertyDescription) ||
          pt.PropertyDescription == propertyDescription)

            //orderby pm.PropertyNo, pm.PartitionNo

            select new
            {
                // ---------------- BASIC ----------------
                pm.Id,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.FlatOrShopName,
                pm.FlatOrShopNo,
                pm.PlotNo,
                pm.PinCode,
                pm.EmailId,
                pm.Address,
                wn.WardNo,
                WardDescription = wn.Description,
                zm.ZoneNo,
                ZoneDescription = zm.Description,
                pm.OwnerTitle,
                pm.OwnerName,
                pm.OccupierTitle,
                pm.OccupierName,

                pt.PropertyDescription,

                sdm.SocietyName,
                sdm.SecretaryName,
                // ---------------- ULB ----------------
                CouncilName = ulb.UlbNameLocal,
                CouncilAddress = ulb.UlbAddress,
                CouncilEmailId = ulb.EmailId,
                CouncilMobileNo = ulb.MobileNo,


            }
        );

        var props = await properties
            .Distinct()
            .OrderBy(x => x.PropertyNo)
            .ThenBy(x => x.PartitionNo)
            .ToListAsync(ct);

        // ---------------- FROM PROPERTY NUMBER FILTER ----------------
        if (int.TryParse(fromPropertyNoText, out var fromPropertyNo))
        {
            props = props
                .Where(x =>
                    int.TryParse(x.PropertyNo, out var no) &&
                    no >= fromPropertyNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(fromPropertyNoText))
        {
            props = props
                .Where(x =>
                    string.Compare(
                        x.PropertyNo,
                        fromPropertyNoText,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        // ---------------- TO PROPERTY NUMBER FILTER ----------------
        if (int.TryParse(toPropertyNoText, out var toPropertyNo))
        {
            props = props
                .Where(x =>
                    int.TryParse(x.PropertyNo, out var no) &&
                    no <= toPropertyNo)
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(toPropertyNoText))
        {
            props = props
                .Where(x =>
                    string.Compare(
                        x.PropertyNo,
                        toPropertyNoText,
                        StringComparison.OrdinalIgnoreCase) <= 0)
                .ToList();
        }

        // TotalTax map for current property set
        var propIds = props.Select(x => x.Id).Distinct().ToList();

        var totalTaxByProperty = await _transRepository.GetQueryable()
            .Where(t => t.FinanceYearId == activeYearId && propIds.Contains(t.PropertyId))
            .GroupBy(t => t.PropertyId)
            .Select(g => new { PropertyId = g.Key, TotalTax = g.Sum(x => x.TaxAmount) })
            .ToDictionaryAsync(x => x.PropertyId, x => x.TotalTax, ct);

        // AMOUNT filter CODE -----------------------------
        if (!string.IsNullOrWhiteSpace(totalTaxFilterType) &&
            !string.IsNullOrWhiteSpace(totalTaxFilterValue) &&
            decimal.TryParse(totalTaxFilterValue, out var filterValue))
        {
            var mode = totalTaxFilterType.Trim().ToLowerInvariant().Replace(" ", "");

            if (mode == "topn" || mode == "top")
            {
                var n = (int)filterValue;
                if (n > 0)
                {
                    props = props
                        .OrderByDescending(p => totalTaxByProperty.GetValueOrDefault(p.Id, 0m))
                        .ThenBy(p => p.PropertyNo)
                        .ThenBy(p => p.FlatOrShopNo)
                        .Take(n)
                        .ToList();
                }
            }
            else if (mode == "lessthan")
            {
                props = props
                    .Where(p => totalTaxByProperty.GetValueOrDefault(p.Id, 0m) < filterValue)
                    .ToList();
            }
            else if (mode == "greaterthan")
            {
                props = props
                    .Where(p => totalTaxByProperty.GetValueOrDefault(p.Id, 0m) > filterValue)
                    .ToList();
            }
        }

        // paging must be last
        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
        props = props.Skip(skip).Take(takePlusOne).ToList();

        var hasMore = take != int.MaxValue && props.Count > take;
        if (hasMore) props = props.Take(take).ToList();

        // ---------------- FINAL CRYSTAL REPORT ROWS ----------------
        var rows = new List<object>();

        foreach (var p in props)
        {
            var row = new Dictionary<string, object?>
            {
                // ---------------- PROPERTY INFO ----------------
                ["ownerId"] = p.Id,
                ["propertyNo"] = p.PropertyNo,
                ["partitionNo"] = p.PartitionNo,
                ["MarathiFlatOrShopNo"] = p.FlatOrShopNo,
                ["FlatOrShopName"] = p.FlatOrShopName,
                ["plotNo"] = p.PlotNo,
                ["PinCode"] = p.PinCode,
                ["EmailId"] = p.EmailId,
                ["OwnerAddress"] = p.Address,
                ["NodeNo"] = p.ZoneNo,
                ["NodeDescription"] = p.ZoneDescription,
                ["WardNo"] = p.WardNo,
                ["WardDescription"] = p.WardDescription,
                ["OwnerTitle"] = p.OwnerTitle,
                ["OwnerName"] = p.OwnerName,
                ["OccupierTitle"] = p.OccupierTitle,
                ["OccupierName"] = p.OccupierName,
                ["PropertyDescription"] = p.PropertyDescription,

                ["MarathiSocietyName"] = p.SocietyName,
                ["SecretaryName"] = p.SecretaryName,
                ["PropertyType"] = p.PropertyDescription,
                ["userName"] = user?.UserName,

                // ---------------- ULB ----------------
                ["CouncilName"] = p.CouncilName,
                ["CouncilAddress"] = p.CouncilAddress,
                ["CouncilEmailId"] = p.CouncilEmailId,
                ["CouncilMobileNo"] = p.CouncilMobileNo,
                ["ownerFullName"] = $"{p.OwnerTitle} {p.OwnerName}".Trim(),
                ["occupierFullName"] = $"{p.OccupierTitle} {p.OccupierName}".Trim(),
                ["NodeSectorNo"] = $"{p.ZoneDescription} {p.WardNo}".Trim(),
                ["wardPropertyropertyZoneNo"] = $"{p.WardNo} {p.PropertyNo} {p.ZoneNo}".Trim(),
            };

            rows.Add(row);
        }
        return rows;
    }

    // ------------------- SUB REPORT SECTION FIELDS -----------------
    private async Task<(List<object> Rows, bool HasMore)> DetailQuery(Guid reportRequestId, Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
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

        // -------- Amount filter inputs -------------
        parameters.TryGetValue("totalTaxFilterType", out var totalTaxFilterType);   // Top n / Less Than / Greater Than
        parameters.TryGetValue("totalTaxFilterValue", out var totalTaxFilterValue); // amount or count (for Top n)

        var financeYear = ParseFinanceYear(parameters);

        var activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);



        // ---------------- PROPERTY QUERY WITH EXTENDED JOINS ----------------
        var propQuery =
            from pm in _propertyRepository.GetQueryable()

                // Existing joins
            join wn in _wardRepository.GetQueryable() on pm.WardId equals wn.Id into wmj
            from wn in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

                // -------- NEW JOINS --------

            join pd in _propertyDetailsRepository.GetQueryable().Where(x => x.IsActive && !x.MarkedForDeletion)
                on pm.Id equals pd.PropertyId into pdj
            from pd in pdj.DefaultIfEmpty()

                // FloorMaster
            join fm in _floorRepository.GetQueryable() on pd.FloorId equals fm.Id into fmj
            from fm in fmj.DefaultIfEmpty()

                // ConstructionTypeMaster
            join ctm in _constructionTypeRepository.GetQueryable() on pd.ConstructionTypeId equals ctm.Id into ctmj
            from ctm in ctmj.DefaultIfEmpty()

            join rvc in _rvCalculationResultsRepository.GetQueryable().Where(x => x.IsActive && !x.MarkedForDeletion)
               on pd.Id equals rvc.PropertyDetailsId into rvcj
            from rvc in rvcj.DefaultIfEmpty()

                // ✅ TypeOfUseMast (TypeOfUseEntity) - JOIN via PropertyDetails.TypeOfUseId
            join tou in _typeOfUseRepository.GetQueryable() on pd.TypeOfUseId equals tou.Id into touj
            from tou in touj.DefaultIfEmpty()

            join rm in _renterMastRepository.GetQueryable().Where(x => x.IsActive && !x.MarkedForDeletion)
           on pd.Id equals rm.PropertyDetailsId into rmj
            from rm in rmj.DefaultIfEmpty()

            where pm.IsActive
                  && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
                  && (zoneId == 0 || wn.ZoneId == zoneId)
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)
                  && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)

            select new
            {
                // Property fields
                pm.Id,
                pm.PropertyNo,
                pm.PartitionNo,

                // PropertyDetails fields
                PropertyDetailsId = pd != null ? pd.Id : (int?)null,
                pd.FloorId,
                pd.ConstructionYear,
                ctm.ConstructionCode,
                pd.CarpetAreaSqMeter,
                pd.CarpetAreaSqFeet,
                tou.Type,

                // FloorMaster fields
                FloorCode = fm != null ? fm.FloorCode : null,

                // RVCalculationResults fields
                RVCalculationResultsId = rvc != null ? rvc.Id : (int?)null,
                rvc.YearlyRate,
                rvc.AnnualRentalValue,
                rvc.Depreciation,
                rvc.DepreciationPer,
                rvc.RateableValue,
                rm.RentMonthly,
                rm.FinalYearlyRent,


            };

        // Execute query and get results from database
        var propsFromDb = await propQuery.ToListAsync(ct);

        var props = propsFromDb
            .GroupBy(x => new { x.Id, x.PropertyDetailsId })
            .Select(g => g.First())
            .OrderBy(x => x.PropertyNo)
            .ThenBy(x => x.PartitionNo)
            .ThenBy(x => x.PropertyDetailsId)
            .ToList();

        // ------------ Property range filter ------------

        // FROM Property No
        if (int.TryParse(fromPropertyNoText, out var fromPropertyNo))
        {
            props = props
                .Where(x =>
                    int.TryParse(x.PropertyNo, out var no) &&
                    no >= fromPropertyNo)
                .ToList();
        }

        // TO Property No
        if (int.TryParse(toPropertyNoText, out var toPropertyNo))
        {
            props = props
                .Where(x =>
                    int.TryParse(x.PropertyNo, out var no) &&
                    no <= toPropertyNo)
                .ToList();
        }

        /////////////////////////////////////////////////////////////////////////////

        var ids = props.Select(x => x.Id).Distinct().ToList();

        // load active tax masters for the page
        var taxMasters = await _taxRepository.GetQueryable()
            .Where(tm => tm.IsActive)
            .Select(tm => new { tm.Id, tm.TaxCode, tm.TaxName, tm.TaxNameAlias, tm.DisplayOrder })
            .OrderBy(tm => tm.DisplayOrder)
            .ToListAsync(ct);

        // Load transaction data WITH CalculationType (RV/CV)
        var transData = await _transRepository.GetQueryable()
            .Where(t => t.FinanceYearId == activeYearId && ids.Contains(t.PropertyId))
            .Select(t => new
            {
                t.PropertyId,
                t.TaxId,
                t.TaxAmount,
                RVorCV = t.CalculationType
            })
            .ToListAsync(ct);

        // Apply RV/CV logic per property
        var transSumsByProperty = transData
            .GroupBy(t => t.PropertyId)
            .Select(g =>
            {
                var taxRows = g.ToList();
                var hasRv = taxRows.Any(t => string.Equals(t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase));
                var hasCv = taxRows.Any(t => string.Equals(t.RVorCV, "CV", StringComparison.OrdinalIgnoreCase));

                bool shouldSkip = hasCv && !hasRv;

                var filteredRows = taxRows;
                if (hasRv && !hasCv)
                {
                    filteredRows = taxRows.Where(t => string.Equals(t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return new
                {
                    PropertyId = g.Key,
                    ShouldSkip = shouldSkip,
                    Transactions = filteredRows
                };
            })
            .ToDictionary(x => x.PropertyId);

        // Filter out CV-only properties
        props = props.Where(p => !transSumsByProperty.ContainsKey(p.Id) || !transSumsByProperty[p.Id].ShouldSkip).ToList();
        ids = props.Select(x => x.Id).Distinct().ToList();

        // Build transaction sums from filtered data
        var transSums = transSumsByProperty
            .Where(kvp => ids.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value.Transactions
                .GroupBy(t => t.TaxId)
                .Select(g => new
                {
                    PropertyId = kvp.Key,
                    TaxId = g.Key,
                    Total = g.Sum(x => x.TaxAmount)
                })
            )
            .ToList();

        // CRITICAL FIX: Ensure transSums only has data for filtered properties
        transSums = transSums.Where(t => ids.Contains(t.PropertyId)).ToList();

        //build map: propertyId->list of taxes(one entry per active tax master; amount = sum or 0)
        var taxByProperty = ids.ToDictionary(
            id => id,
            id => taxMasters.Select(tm =>
            {
                var s = transSums.FirstOrDefault(x => x.PropertyId == id && x.TaxId == tm.Id);
                return new
                {
                    TaxId = tm.Id,
                    TaxCode = tm.TaxCode ?? string.Empty,
                    TaxName = tm.TaxName ?? string.Empty,
                    TaxNameAlias = tm.TaxNameAlias ?? string.Empty,
                    DisplayOrder = tm.DisplayOrder,
                    TaxAmount = s?.Total ?? 0m

                };
            })
            .OrderBy(t => t.DisplayOrder)
            .ToList()
        );

        // totals per property
        var totalTaxByProperty = taxByProperty.ToDictionary(x => x.Key, x => x.Value.Sum(t => t.TaxAmount));

        //  AMOUNT filter Code
        if (!string.IsNullOrWhiteSpace(totalTaxFilterType) &&
            !string.IsNullOrWhiteSpace(totalTaxFilterValue) &&
            decimal.TryParse(totalTaxFilterValue, out var filterValue))
        {
            var mode = totalTaxFilterType.Trim().ToLowerInvariant().Replace(" ", "");

            if (mode == "topn" || mode == "top")
            {
                var n = (int)filterValue;
                if (n > 0)
                {
                    props = props
                        .OrderByDescending(p => totalTaxByProperty.GetValueOrDefault(p.Id, 0m))
                        .ThenBy(p => p.PropertyNo)
                        .ThenBy(p => p.PartitionNo)
                        .Take(n)
                        .ToList();
                }
            }
            else if (mode == "lessthan")
            {
                props = props
                    .Where(p => totalTaxByProperty.GetValueOrDefault(p.Id, 0m) < filterValue)
                    .ToList();
            }
            else if (mode == "greaterthan")
            {
                props = props
                    .Where(p => totalTaxByProperty.GetValueOrDefault(p.Id, 0m) > filterValue)
                    .ToList();
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////

        // Paging must be after TotalTax filter
        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
        props = props.Skip(skip).Take(takePlusOne).ToList();

        var hasMore = take != int.MaxValue && props.Count > take;
        if (hasMore)
            props = props.Take(take).ToList();

        var rows = new List<object>();

        // -------- PIVOT LOGIC: ONE ROW PER PROPERTY --------
        foreach (var p in props)
        {
            if (!taxByProperty.TryGetValue(p.Id, out var taxes))
                continue;

            decimal totalTax = taxes.Sum(x => x.TaxAmount);

            // Build base row with property data
            var row = new Dictionary<string, object?>
            {
                ["ownerId"] = p.Id,
                ["propertyNo"] = p.PropertyNo,
                ["partitionNo"] = p.PartitionNo,

                //// Total tax
                //["TotalTax"] = totalTax.ToString("0"),
                //["TotalTaxAmount"] = totalTax,

                // PropertyDetails
                ["ConstructionYear"] = p.ConstructionYear,
                ["ConstructionCode"] = p.ConstructionCode,
                ["FloorCode"] = p.FloorCode,
                ["TypeOfUse"] = p.Type,

                ["CarpetAreaSqMeter"] = p.CarpetAreaSqMeter?.ToString("0"),
                ["CarpetAreaSqFeet"] = p.CarpetAreaSqFeet?.ToString("0"),

                // RVCalculationResults
                ["YearlyRate"] = p.YearlyRate?.ToString("0"),
                ["AnnualRentalValue"] = p.AnnualRentalValue?.ToString("0"),
                ["Depreciation"] = p.Depreciation,
                ["DepreciationPer"] = p.DepreciationPer,
                ["RateableValue"] = p.RateableValue?.ToString("0"),
                ["RentMonthly"] = p.RentMonthly?.ToString("0"),
                ["FinalYearlyRent"] = p.FinalYearlyRent?.ToString("0"),
                //["TotalTax"] = totalTax.ToString("0"),
                // Total tax
                ["TotalTax"] = totalTax.ToString("0")
            };

            // -------- TAX CODE TO FIELD NAME MAPPING --------
            var taxCodeToFieldName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GEN"] = "GEN_TAX",              // General Tax (DisplayOrder: 1)
                ["STATE_EDU"] = "STATE_EDU_TAX",  // State Education Tax (DisplayOrder: 2)
                ["STATE_EMP"] = "STATE_EMP_TAX",  // State Employment Tax (DisplayOrder: 3)
                ["TREE"] = "TREE_TAX",            // Tree Cess (DisplayOrder: 4)
                ["SP_WATER"] = "SP_WATER_TAX",    // Special Water Cess (DisplayOrder: 5)
                ["ROAD"] = "ROAD_TAX",            // Road Cess (DisplayOrder: 6)
                ["FIRE"] = "FIRE_TAX",            // Fire Cess (DisplayOrder: 7)
                ["LIGHT"] = "LIGHT_TAX",          // Light Cess (DisplayOrder: 8)
                ["WATER_BEN"] = "WATER_BEN_TAX",  // Water Benefit Cess (DisplayOrder: 9)
                ["SEWAGE"] = "SEWAGE_TAX",        // Sewage Disposal Cess (DisplayOrder: 10)
                ["SP_E DU"] = "SP_EDU_TAX",       // Special Education Tax (DisplayOrder: 11) ← NOTE: Space in TaxCode
                ["TaxTotal"] = "TaxTotal"
            };

            // Initialize all tax fields to 0
            foreach (var fieldName in taxCodeToFieldName.Values)
            {
                row[fieldName] = 0m;
            }

            // Fill in actual tax values based on TaxCode
            foreach (var tax in taxes)
            {
                if (taxCodeToFieldName.TryGetValue(tax.TaxCode, out var fieldName))
                {
                    row[fieldName] = Math.Round(tax.TaxAmount).ToString("0");  // ✅ Rounded whole number
                }
            }
            rows.Add(row);
        }

        return (rows, hasMore);
    }
}
