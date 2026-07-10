using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
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

        public string ProviderCode => "NoticeNewDataProvider";

        private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
        private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
        private readonly IReportDataRepository<WardEntity> _wardRepository;
        private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
        private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
        private readonly IReportDataRepository<TaxPendingDetailsEntity> _taxPendingDetailsRepository;
        private readonly IReportDataRepository<YearMasterEntity> _yearMastRepository;
        private readonly IReportDataRepository<TaxMasterEntity> _taxMastRepository;

        public NoticeNewDataProvider(
            IReportDataRepository<PropertyEntity> propertyRepository,
            IReportDataRepository<ZoneEntity> zoneRepository,
            IReportDataRepository<WardEntity> wardRepository,
            IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
            IReportDataRepository<TransMastEntity> transMastRepository,
            IReportDataRepository<TaxPendingDetailsEntity> taxPendingDetailsRepository,
            IReportDataRepository<YearMasterEntity> yearMastRepository,
            IReportDataRepository<TaxMasterEntity> taxMastRepository)
        {
            _propertyRepository = propertyRepository;
            _zoneRepository = zoneRepository;
            _wardRepository = wardRepository;
            _propertyDetailsRepository = propertyDetailsRepository;
            _transmastRepository = transMastRepository;
            _taxPendingDetailsRepository = taxPendingDetailsRepository;
            _yearMastRepository = yearMastRepository;
            _taxMastRepository = taxMastRepository;
        }

        // Static — never runs a query (avoids the heavy pivot executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection, true),
        };

        public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            // Full result (used only off the worker hot path). Bounded internally per page.
            var (rows, _) = await BuildPageAsync(parameters, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 100;

            if (!section.Equals(MainSection, StringComparison.OrdinalIgnoreCase))
            {
                return new ReportDataPage
                {
                    Section = section,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    HasMore = false,
                    Rows = Array.Empty<object>()
                };
            }

            var (rows, hasMore) = await BuildPageAsync(parameters, (page - 1) * pageSize, pageSize, ct);
            return new ReportDataPage
            {
                Section = MainSection,
                Page = page,
                PageSize = pageSize,
                TotalCount = -1, // not computed (avoids a per-page COUNT); HasMore drives the loop
                HasMore = hasMore,
                Rows = rows,
            };
        }

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            parameters.TryGetValue("wardId", out var wardIdStr);
            parameters.TryGetValue("propertyNo", out var propertyNo);
            parameters.TryGetValue("partitionNo", out var partitionNo);
            int.TryParse(wardIdStr, out var wardId);

            var activeFinanceYearId = await _yearMastRepository.GetQueryable()
                .Where(ym => ym.IsActive)
                .Select(ym => ym.Id)
                .FirstOrDefaultAsync(ct);

            var trans = _transmastRepository.GetQueryable();
            var pdQuery = _propertyDetailsRepository.GetQueryable();

            // Page over DISTINCT properties (one output row each) that have a tax assessment for the
            // active year. Fetch take+1 to derive HasMore without a separate COUNT.
            var propPage = from pm in _propertyRepository.GetQueryable()
                           where pm.IsActive
                              && (wardId == 0 || pm.WardId == wardId)
                              && (string.IsNullOrEmpty(propertyNo) || pm.PropertyNo == propertyNo)
                              && (string.IsNullOrEmpty(partitionNo) || pm.PartitionNo == partitionNo)
                              && trans.Any(t => t.PropertyId == pm.Id && t.FinanceYearId == activeFinanceYearId)
                           join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wmj
                           from wm in wmj.DefaultIfEmpty()
                           join zm in _zoneRepository.GetQueryable() on pm.TaxZoneId equals zm.Id into zmj
                           from zm in zmj.DefaultIfEmpty()
                           orderby pm.PropertyNo, pm.PartitionNo
                           select new
                           {
                               id = pm.Id,
                               wardId = pm.WardId,
                               wardNo = wm != null ? wm.WardNo : null,
                               zoneNo = zm != null ? zm.ZoneNo : null,
                               propertyNo = pm.PropertyNo,
                               partitionNo = pm.PartitionNo,
                               ownerName = pm.OwnerName,
                               address = pm.Address,
                               mobileNo = pm.MobileNo,
                               carpetAreaSqMeter = pdQuery.Where(pd => pd.PropertyId == pm.Id).Sum(pd => (decimal?)pd.CarpetAreaSqMeter),
                               builtupAreaSqMeter = pdQuery.Where(pd => pd.PropertyId == pm.Id).Sum(pd => (decimal?)pd.BuiltupAreaSqMeter),
                           };

            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var props = await propPage.Skip(skip).Take(takePlusOne).ToListAsync(ct);

            var hasMore = take != int.MaxValue && props.Count > take;
            if (hasMore) props = props.Take(take).ToList();

            var ids = props.Select(p => p.id).ToList();

            // Tax rows only for this page's properties.
            var taxRows = await (
                from t in trans.Where(t => t.FinanceYearId == activeFinanceYearId && ids.Contains(t.PropertyId))
                join taxm in _taxMastRepository.GetQueryable() on t.TaxId equals taxm.Id
                join tpd in _taxPendingDetailsRepository.GetQueryable().Where(p => p.PendingYearId == activeFinanceYearId)
                    on new { t.PropertyId, t.TaxId } equals new { tpd.PropertyId, tpd.TaxId } into tpdj
                from tpd in tpdj.DefaultIfEmpty()
                select new
                {
                    t.PropertyId,
                    taxm.TaxCode,
                    taxm.DisplayOrder,
                    t.TaxAmount,
                    pendingAmount = tpd != null ? tpd.PendingAmount : (decimal?)null,
                }).ToListAsync(ct);

            var taxByProperty = taxRows
                .GroupBy(r => r.PropertyId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DisplayOrder).ToList());

            var rows = props.Select(p =>
            {
                var row = new Dictionary<string, object?>
                {
                    ["id"] = p.id,
                    ["wardId"] = p.wardId,
                    ["wardNo"] = p.wardNo,
                    ["zoneNo"] = p.zoneNo,
                    ["propertyNo"] = p.propertyNo,
                    ["partitionNo"] = p.partitionNo,
                    ["ownerName"] = p.ownerName,
                    ["address"] = p.address,
                    ["mobileNo"] = p.mobileNo,
                    ["carpetAreaSqMeter"] = p.carpetAreaSqMeter,
                    ["builtupAreaSqMeter"] = p.builtupAreaSqMeter,
                    ["activeFinanceYearId"] = activeFinanceYearId,
                };
                if (taxByProperty.TryGetValue(p.id, out var taxes))
                {
                    foreach (var tax in taxes)
                    {
                        var safeCode = tax.TaxCode?.Replace(' ', '_') ?? "UNKNOWN";
                        row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                        row[$"TaxPending_{safeCode}"] = tax.pendingAmount;
                    }
                }
                return (object)row;
            }).ToList();

            return (rows, hasMore);
        }
    }
}
