using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
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
        IReportDataRepository<PropertyTypeMasterEntity> PropertyTypeMasterRepository)
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
        var mainRows = await MainQuery(financeYear, parameters, 0, int.MaxValue, ct);
        var (detailRows, _) = await DetailQuery(parameters, 0, int.MaxValue, ct);
        return new { main = mainRows, NoticeBill = detailRows };
    }

    public async Task<ReportDataPage> GetDataPageAsync(Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
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
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = int.MaxValue;

            var skip = pageSize == int.MaxValue ? 0 : (page - 1) * pageSize;
            var rows = await MainQuery(financeYear, parameters, skip, pageSize, ct);
            return new ReportDataPage
            {
                Section = MainSection,
                Page = 1,
                PageSize = rows.Count,
                TotalCount = rows.Count,
                HasMore = false,
                Rows = rows.Cast<object>().ToList(),
            };
        }

        if (section.Equals(DetailSection, StringComparison.OrdinalIgnoreCase))
        {
            if (page < 1) page = 1;

            var skip = (page - 1) * pageSize;

            var (rows, hasMore) = await DetailQuery(
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

        // Unknown section → empty page.
        return new ReportDataPage { Section = section, Page = page, PageSize = pageSize, HasMore = false };
    }

    private static short ParseFinanceYear(Dictionary<string, string> parameters)
    {
        parameters.TryGetValue("financeYear", out var financeYearStr);
        short.TryParse(financeYearStr, out var financeYear);
        return financeYear;
    }

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

    //private IQueryable<YearMasterEntity> BaseQuery(short financeYear) =>
    //    _yearRepository.GetQueryable()
    //        .Where(b => financeYear == 0 || b.Year == financeYear);
    private IQueryable<YearMasterEntity> BaseQuery(short financeYear) =>
    _yearRepository.GetQueryable()
        .Where(b => financeYear == 0 ? b.IsActive : b.Year == financeYear);

    // ------------------- MAIN SECTION REPORT FIELDS -----------------
    private async Task<List<object>> MainQuery(short financeYear, Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        var activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);

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

            join w in _wingRepository.GetQueryable() on sdm.WingId equals w.Id into wingj
            from w in wingj.DefaultIfEmpty()

            join pt in _PropertyTypeMasterRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id into ptj
            from pt in ptj.DefaultIfEmpty()

            join pmo in _propertyOldRepository.GetQueryable() on pm.PropertyMastOldId equals pmo.Id into pmoj
            from pmo in pmoj.DefaultIfEmpty()

            from ulb in _ulbMasterRepository.GetQueryable()
                .Where(x => x.IsActive)
                .Take(1)

            where pm.IsActive
                  && (zoneId == 0 || wn.ZoneId == zoneId)
                  && (wardId == 0 || pm.WardId == wardId)
                  && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                  && (partitionNoText == null || pm.PartitionNo == partitionNoText)

            orderby pm.PropertyNo, pm.PartitionNo

            select new
            {
                // ---------------- BASIC ----------------
                pm.Id,
                pm.UPICId,
                pm.OwnerName,
                pm.OwnerNameEnglish,
                pm.Address,
                pm.PropertyNo,
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
                CalculationValue = _transRepository.GetQueryable()
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
                pmo.OldPropertyNo,
                pt.PropertyDescription
            }
        );
        // --------------------------------------------------------------------------
        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;

        var props = await properties .Skip(skip) .Take(takePlusOne) .ToListAsync(ct);

        var hasMore = take != int.MaxValue && props.Count > take;

        if (hasMore)
            props = props.Take(take).ToList();

        //var ids = props.Select(x => x.Id).ToList();

        // ---------------- FINAL CRYSTAL REPORT ROWS ----------------
        var rows = new List<object>();

        foreach (var p in props)
        {
            //var amountInWords = Convert.ToInt64(p.CalculationValue).ToWords();
            var amountInWords = AmountToWords(p.CalculationValue);

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
                ["TotalCapitalValue"] = p.CalculationValue,
                ["TotalCapitalValueInWords"] = amountInWords,

                ["wardId"] = wardId,
                ["PartitionNo"] = "",
                ["financeYear"] = "",

                // FOR PANVEL NOTICE NEW REPORT
                ["PropertyType"] = p.PropertyDescription,
                ["OccupierName"] = p.OccupierName,
                ["OwnerMobileNo"] = p.MobileNo,
                ["OldPropertyNo"] = p.OldPropertyNo
            };

            rows.Add(row);
        }
        return rows;
    }

    // ------------------- SUB REPORT SECTION FIELDS -----------------
    private async Task<(List<object> Rows, bool HasMore)> DetailQuery(Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
    {
        parameters.TryGetValue("zoneId", out var zoneIdText);
        int.TryParse(zoneIdText, out var zoneId);

        parameters.TryGetValue("wardId", out var wardIdText);
        int.TryParse(wardIdText, out var wardId);

        parameters.TryGetValue("propertyNo", out var propertyNoText);
        propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

        parameters.TryGetValue("partitionNo", out var partitionNoText);
        partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

        var financeYear = ParseFinanceYear(parameters);

        var activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);

        // ---------------- PROPERTY QUERY ----------------
        var propQuery =
            from pm in _propertyRepository.GetQueryable()

            join pmo in _propertyOldRepository.GetQueryable() on pm.PropertyMastOldId equals pmo.Id into pmoj
            from pmo in pmoj.DefaultIfEmpty()

            join wn in _wardRepository.GetQueryable() on pm.WardId equals wn.Id into wmj
            from wn in wmj.DefaultIfEmpty()

            join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()

            //join tm in _transRepository.GetQueryable() on pm.Id equals tm.PropertyId into tmj
            //from tm in tmj.DefaultIfEmpty()

            where pm.IsActive
                   && (zoneId == 0 || wn.ZoneId == zoneId)
                    && (wardId == 0 || pm.WardId == wardId)
                    && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                    && (partitionNoText == null || pm.PartitionNo == partitionNoText)

            orderby pm.PropertyNo, pm.PartitionNo

            select new
            {
                pm.Id,
                pm.UPICId,
                pm.OwnerName,
                pm.OwnerNameEnglish,
                pm.PropertyNo,
                wn.WardNo,
                zm.ZoneNo,
                //tm.TaxAmount
            };

        var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;

        var props = await propQuery.Skip(skip).Take(takePlusOne).ToListAsync(ct);

        var hasMore = take != int.MaxValue && props.Count > take;

        if (hasMore)
            props = props.Take(take).ToList();

        var ids = props.Select(x => x.Id).ToList();

        // ---------------- TAX QUERY ----------------
        var taxRows = await (
            from t in _transRepository.GetQueryable()
            join taxm in _taxRepository.GetQueryable() on t.TaxId equals taxm.Id

            where t.FinanceYearId == activeYearId
                && ids.Contains(t.PropertyId)
                && taxm.IsActive

            select new
            {
                t.PropertyId,
                taxm.TaxName,
                taxm.DisplayOrder,
                t.TaxAmount
            }).ToListAsync(ct);

        // ---------------- GROUP TAX ----------------
        var taxByProperty = taxRows
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.DisplayOrder).ToList()
            );

        // ---------------- FINAL FLAT ROWS (CRYSTAL REPORT READY) ----------------
        var rows = new List<object>();

        foreach (var p in props)
        {
            if (taxByProperty.TryGetValue(p.Id, out var taxes))
            {
                decimal totalFirstHalf = 0;
                decimal totalSecondHalf = 0;

                foreach (var tax in taxes)
                {
                    var first = tax.TaxAmount / 2m;
                    var second = tax.TaxAmount - first;

                    totalFirstHalf += first;
                    totalSecondHalf += second;
                }

                decimal TotalTax = taxes.Sum(x => x.TaxAmount);

                var totalFirstHalfInWords = AmountToWords(totalFirstHalf);
                var totalSecondHalfInWords = AmountToWords(totalSecondHalf);
                var TotalTaxinWords = AmountToWords(TotalTax);

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
                        ["NodeNo"] = p.ZoneNo,
                        ["NewWardNo"] = p.WardNo,

                        // ---------------- TAX FIELDS ----------------
                        ["TaxName"] = t.TaxName,

                        ["FirstHalf"] = firstHalf.ToString("0"),
                        ["SecondHalf"] = secondHalf.ToString("0"),

                        // ---------------- TOTAL FIELDS TOTAL ----------------
                        ["TotalFirstHalf"] = totalFirstHalf.ToString("0"),
                        ["TotalSecondHalf"] = totalSecondHalf.ToString("0"),

                        // ---------------- TOTALS IN WORDS ----------------
                        ["TotalFirstHalfInWords"] = totalFirstHalfInWords,
                        ["TotalSecondHalfInWords"] = totalSecondHalfInWords,

                        ["FirstHalfLastPaymentDate"] = "30/11/2026",
                        ["SecondHalfLastPaymentDate"] = "31/12/2026",

                        ["wardId"] = " ",
                        ["PartitionNo"] = " ",
                        ["financeYear"] = " ",

                        //FOR PANVEL NOTICE NEW REPORT
                        ["TotalTax"] = TotalTax.ToString("0"),
                        ["TotalTaxinWords"] = TotalTaxinWords,
                        //["TaxAmount"] = p.TaxAmount,
                        ["TaxAmount"] = t.TaxAmount
                    };

                    rows.Add(row);
                }
            }
        }
        return (rows, hasMore);
    }

}
