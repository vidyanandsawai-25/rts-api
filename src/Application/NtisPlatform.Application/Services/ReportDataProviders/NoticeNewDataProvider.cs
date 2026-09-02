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
public class NoticeNewDataProvider : IPagedReportDataProvider
{
    public const string MainSection = "main";
    public const string DetailSection = "NoticeBill";
    public const string ImageParamsSection = "_reportImageParams";
    public string ProviderCode => "NoticeNewDataProvider";

    private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
    private readonly IReportDataRepository<PropertyImagesMastEntity> _propertyImagesRepository;
    private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
    private readonly IReportDataRepository<WardEntity> _wardRepository;
    private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
    private readonly IReportDataRepository<WingEntity> _wingRepository;
    private readonly IReportDataRepository<PropertyMastOldEntity> _propertyOldRepository;
    private readonly IReportDataRepository<TransMastEntity> _transRepository;
    private readonly IReportDataRepository<TaxMasterEntity> _taxRepository;
    private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
    private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
    private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
    private readonly IReportDataRepository<PropertyTypeMasterEntity> _PropertyTypeMasterRepository;
    //private readonly IReportDataRepository<ReportRequestEntity> _ReportRequestRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
    private readonly IReportDataRepository<UserEntity> _userRepository;
    private readonly IReportDataRepository<PropertyMapMasterEntity> _PropertyMapRepository;
    private readonly IReportDataRepository<PropertyMapDetailEntity> _PropertyMapDetailRepository;
    private readonly IReportDataRepository<RVCalculationResultsEntity> _rvCalculationResultsRepository;

    public NoticeNewDataProvider(
        IReportDataRepository<PropertyEntity> propertyRepository,
        IReportDataRepository<PropertyImagesMastEntity> propertyImagesRepository,
        IReportDataRepository<ZoneEntity> zoneRepository,
        IReportDataRepository<WardEntity> wardRepository,
        IReportDataRepository<SocietyDetailsEntity> societyRepository,
        IReportDataRepository<WingEntity> wingRepository,
        IReportDataRepository<PropertyMastOldEntity> propertyOldRepository,
        IReportDataRepository<TransMastEntity> transRepository,
        IReportDataRepository<TaxMasterEntity> taxRepository,
        IReportDataRepository<YearMasterEntity> yearRepository,
        IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
        IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
        IReportDataRepository<PropertyTypeMasterEntity> PropertyTypeMasterRepository,
        //IReportDataRepository<ReportRequestEntity> ReportRequestRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportDataRepository<UserEntity> userRepository,
        IReportDataRepository<PropertyMapMasterEntity> PropertyMapRepository,
        IReportDataRepository<PropertyMapDetailEntity> PropertyMapDetailRepository,
        IReportDataRepository<RVCalculationResultsEntity> rvCalculationResultsRepository)
    {
        _propertyRepository = propertyRepository;
        _propertyImagesRepository = propertyImagesRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _wingRepository = wingRepository;
        _propertyOldRepository = propertyOldRepository;
        _transRepository = transRepository;
        _taxRepository = taxRepository;
        _yearRepository = yearRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _ulbMasterRepository = ulbMasterRepository;
        _PropertyTypeMasterRepository = PropertyTypeMasterRepository;
        _ReportRequestRepository = reportRequestRepository;
        _userRepository = userRepository;
        _PropertyMapRepository = PropertyMapRepository;
        _PropertyMapDetailRepository = PropertyMapDetailRepository;
        _rvCalculationResultsRepository = rvCalculationResultsRepository;
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
        return new { main = mainRows, NoticeBill = detailRows };
    }

    public async Task<ReportDataPage> GetDataPageAsync(
    Guid reportRequestId,
    Dictionary<string, string> parameters,
    string section,
    int page,
    int pageSize,
    CancellationToken ct = default)
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

    private static string AmountToWords(decimal amount) => NumberToWords((long)decimal.Truncate(amount));
    private static string NumberToWords(long n)
    {
        if (n == 0) return "zero";
        if (n < 0) return "minus " + NumberToWords(-n);

        string[] u = ["", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
                  "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"];
        string[] t = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];

        if (n < 20) return u[n];
        if (n < 100) return t[n / 10] + (n % 10 == 0 ? "" : " " + u[n % 10]);
        if (n < 1000) return u[n / 100] + " hundred" + (n % 100 == 0 ? "" : " " + NumberToWords(n % 100));
        if (n < 1_000_000) return NumberToWords(n / 1000) + " thousand" + (n % 1000 == 0 ? "" : " " + NumberToWords(n % 1000));
        if (n < 1_000_000_000) return NumberToWords(n / 1_000_000) + " million" + (n % 1_000_000 == 0 ? "" : " " + NumberToWords(n % 1_000_000));
        return NumberToWords(n / 1_000_000_000) + " billion" + (n % 1_000_000_000 == 0 ? "" : " " + NumberToWords(n % 1_000_000_000));
    }

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

            //// ✅ ADD THIS JOIN
            //join rvc in _rvCalculationResultsRepository.GetQueryable() on pm.Id equals rvc.PropertyId into rvcj
            //from rvc in rvcj.DefaultIfEmpty()
            join rvcGroup in (
    from r in _rvCalculationResultsRepository.GetQueryable()
    where r.IsActive && !r.MarkedForDeletion
    group r by r.PropertyId into g
    select new
    {
        PropertyId = g.Key,
        RateableValue = g.Max(x => x.RateableValue)  // ✅ Single value per property
    }
) on pm.Id equals rvcGroup.PropertyId into rvcj
            from rvc in rvcj.DefaultIfEmpty()

            where pm.IsActive

            join wn in _wardRepository.GetQueryable() on pm.WardId equals wn.Id into wmj
            from wn in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

            join sdm in _societyRepository.GetQueryable() on pm.Id equals sdm.PropertyId into sdmj
            from sdm in sdmj.DefaultIfEmpty()

            join w in _wingRepository.GetQueryable() on sdm.WingId equals w.Id into wingj
            from w in wingj.DefaultIfEmpty()

            join pt in _PropertyTypeMasterRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id into ptj
            from pt in ptj.DefaultIfEmpty()

                //----------- ProperyMastOld join --------------
            join pmd in _PropertyMapDetailRepository.GetQueryable() on pm.Id equals pmd.PropertyIdNew into pmdj
            from pmd in pmdj.DefaultIfEmpty()

            join pmm in _PropertyMapRepository.GetQueryable() on pmd.PropertyMapId equals pmm.Id into pmmj
            from pmm in pmmj.DefaultIfEmpty()

            join pmo in _propertyOldRepository.GetQueryable() on pmd.PropertyIdOld equals pmo.Id into oldj
            from pmo in oldj.DefaultIfEmpty()

            from ulb in _ulbMasterRepository.GetQueryable()
                .Where(x => x.IsActive)
                .Take(1)

            where pm.IsActive
      // && (rvc == null || (rvc.IsActive && !rvc.MarkedForDeletion))
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
                pm.UPICId,
                pm.OwnerName,
                pm.OwnerNameEnglish,
                pm.Address,
                pm.PropertyNo,
                pm.PartitionNo,
                pm.FlatOrShopName,
                pm.FlatOrShopNo,
                pm.CSN,
                w.WingNo,
                pm.PlotNo,
                pm.Location,
                sdm.SocietyName,
                wn.WardNo,
                zm.ZoneNo,
                pm.CreatedDate,
                RVorCVValue = _transRepository.GetQueryable()
                .Where(t => t.PropertyId == pm.Id && t.FinanceYearId == activeYearId)
                .Select(t => (decimal?)t.CalculationValue)
                .FirstOrDefault() ?? 0m,

                // ---------------- ULB ----------------
                CouncilName = ulb.UlbName,
                CouncilAddress = ulb.UlbAddress,
                CouncilEmailId = ulb.EmailId,
                CouncilMobileNo = ulb.MobileNo,

                // FOR PANVEL NOTICE NEW REPORT
                pm.OccupierName,
                pm.MobileNo,
                pt.PropertyDescription,

                //PropertyMastOld Fields
                pmo.OldPropertyNo,
                pmo.OldPartitionNo,

                rvc.RateableValue
            }
        );
        // --------------------------------------------------------------------------
        //var props = await properties.ToListAsync(ct);
        // ✅ FIX: Add Distinct() and OrderBy AFTER Distinct
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
        ////////////////////////////////////////////////////////

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
            //var amountInWords = Convert.ToInt64(p.RVorCVValue).ToWords();
            var amountInWords = AmountToWords(p.RVorCVValue);

            // ---------------- GET OLD PROPERTY Fields ----------------
            var oldProperty = await _propertyOldRepository.GetQueryable()
            .Where(x => x.Id == p.Id)
            .Select(x => new
            {
                x.OldPropertyNo,
                x.OldPartitionNo
            })
            .FirstOrDefaultAsync(ct);

            var row = new Dictionary<string, object?>
            {
                // ---------------- IDENTIFIER ----------------
                ["OwnerId"] = p.Id,
                ["UPICId"] = p.UPICId,

                // ---------------- ULB ----------------
                ["CouncilName"] = p.CouncilName,
                ["CouncilAddress"] = p.CouncilAddress,
                ["CouncilEmailId"] = p.CouncilEmailId,
                ["CouncilMobileNo"] = p.CouncilMobileNo,

                // ---------------- STATIC FIELDS ----------------
                ["PropertyTaxYear"] = "2025 -2026",
                ["BillNo"] = "202610BIL13817249",
                ["BillDate"] = "01/09/2026",
                ["BillingStartDate"] = "01/04/2026",
                ["BillingEndDate"] = "31/03/2027",

                // ---------------- PROPERTY INFO ----------------
                ["MarathiOwnerName"] = p.OwnerName,
                ["OwnerName"] = p.OwnerNameEnglish,
                ["MarathiOwnerAddress"] = p.Address,

                ["PropertyNo"] = p.PropertyNo,
                ["PartitionNo"] = p.PartitionNo,
                ["CSN"] = p.CSN,
                ["MarathiOwnerDukanFlatNo"] = p.FlatOrShopNo,
                ["PropertyName"] = p.FlatOrShopName,
                ["NodeNo"] = p.ZoneNo,
                ["NewWardNo"] = p.WardNo,
                ["Wing"] = p.WingNo,
                ["PlotNo"] = p.PlotNo,
                ["Location"] = p.Location,
                ["MarathiSocietyName"] = p.SocietyName,

                ["FirstTaxAssessmentDate"] = p.CreatedDate?.Year.ToString(),
                ["TotalCapitalValue"] = p.RVorCVValue,
                ["TotalCapitalValueInWords"] = amountInWords,

                // FOR PANVEL NOTICE NEW REPORT
                ["PropertyType"] = p.PropertyDescription,
                ["OccupierName"] = p.OccupierName,
                ["OwnerMobileNo"] = p.MobileNo,
                ["OldPropertyNo"] = oldProperty?.OldPropertyNo,
                ["userName"] = user?.UserName,

                ["RateableValue"] = p.RateableValue?.ToString("0"),

                ["NodeWardInfo"] = $"{p.ZoneNo}-{p.WardNo}",
                ["FlatInfo"] = $"{p.WingNo}-{p.FlatOrShopNo}"

                // PropertyMastOld Fields
                //["OldPropertyNo"] = p.OldPropertyNo,
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

        // ---------------- PROPERTY QUERY ----------------
        var propQuery =
            from pm in _propertyRepository.GetQueryable()
            join wn in _wardRepository.GetQueryable() on pm.WardId equals wn.Id into wmj
            from wn in wmj.DefaultIfEmpty()
            join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()
            where pm.IsActive
                  && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
                  && (zoneId == 0 || wn.ZoneId == zoneId)
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)
                  && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)
            select new
            {
                pm.Id,
                pm.UPICId,
                pm.OwnerName,
                pm.OwnerNameEnglish,
                pm.PropertyNo,
                pm.PartitionNo,
                wn.WardNo,
                zm.ZoneNo
            };

        // Distinct first, OrderBy after Distinct (EF warning fix)
        var props = await propQuery
            .Distinct()
            .OrderBy(x => x.PropertyNo)
            .ThenBy(x => x.PartitionNo)
            .ToListAsync(ct);

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
            .Select(tm => new { tm.Id, tm.TaxCode, tm.TaxName, tm.DisplayOrder })
            .OrderBy(tm => tm.DisplayOrder)
            .ToListAsync(ct);

        // load transaction sums for the selected properties & finance year
        var transSums = await _transRepository.GetQueryable()
            .Where(t => t.FinanceYearId == activeYearId && ids.Contains(t.PropertyId))
            .GroupBy(t => new { t.PropertyId, t.TaxId })
            .Select(g => new { g.Key.PropertyId, g.Key.TaxId, Total = g.Sum(x => x.TaxAmount) })
            .ToListAsync(ct);

        //build map: propertyId->list of taxes(one entry per active tax master; amount = sum or 0)
        var taxByProperty = ids.ToDictionary(
            id => id,
            id => taxMasters.Select(tm =>
            {
                var s = transSums.FirstOrDefault(x => x.PropertyId == id && x.TaxId == tm.Id);
                return new
                {
                    TaxName = tm.TaxName ?? string.Empty,
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

        foreach (var p in props)
        {
            if (!taxByProperty.TryGetValue(p.Id, out var taxes))
                continue;

            decimal totalFirstHalf = 0;
            decimal totalSecondHalf = 0;

            foreach (var tax in taxes)
            {
                var first = tax.TaxAmount / 2m;
                var second = tax.TaxAmount - first;
                totalFirstHalf += first;
                totalSecondHalf += second;
            }

            decimal totalTax = taxes.Sum(x => x.TaxAmount);
            var totalTaxInWords = AmountToWords(totalTax);


            foreach (var t in taxes)
            {
                var firstHalf = t.TaxAmount / 2m;
                var secondHalf = t.TaxAmount - firstHalf;

                var row = new Dictionary<string, object?>
                {
                    ["OwnerId"] = p.Id,
                    ["UPICId"] = p.UPICId,
                    ["MarathiOwnerName"] = p.OwnerName,
                    ["OwnerName"] = p.OwnerNameEnglish,
                    ["PropertyNo"] = p.PropertyNo,
                    ["PartitionNo"] = p.PartitionNo,
                    ["NodeNo"] = p.ZoneNo,
                    ["NewWardNo"] = p.WardNo,

                    // ---------------- TAX FIELDS ----------------
                    ["TaxName"] = t.TaxName,

                    ["FirstHalf"] = firstHalf.ToString("0"),
                    ["SecondHalf"] = secondHalf.ToString("0"),

                    // ---------------- TOTAL FIELDS TOTAL ----------------
                    ["TotalFirstHalf"] = totalFirstHalf.ToString("0"),
                    ["TotalSecondHalf"] = totalSecondHalf.ToString("0"),

                    ["FirstHalfLastPaymentDate"] = "30/11/2026",
                    ["SecondHalfLastPaymentDate"] = "31/12/2026",

                    ["TotalTax"] = totalTax.ToString("0"),
                    ["TotalTaxinWords"] = totalTaxInWords,
                    ["TaxAmount"] = t.TaxAmount
                };

                rows.Add(row);

            }
        }
        return (rows, hasMore);
    }

}
