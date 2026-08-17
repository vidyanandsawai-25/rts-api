using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// WarrentNotice report data provider.
    /// Supports parameter filtering for Year, ZoneNo, WardNo, FromPropertyNo, ToPropertyNo, PartitionNo,
    /// Amount, LessThanGreaterThan, Property Type, Property Description, Assessment Type, and Top N.
    /// </summary>
    public class WarrentNoticeDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";

        public string ProviderCode => "WarrentNoticeDataProvider";

        private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
        private readonly IReportDataRepository<WardEntity> _wardRepository;
        private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeMasterRepository;
        private readonly IReportDataRepository<TransMastEntity> _transRepository;
        private readonly IReportDataRepository<TaxPendingDetailsEntity> _taxPendingRepository;
        private readonly IReportDataRepository<UserEntity> _userRepository;
        private readonly IReportDataRepository<YearMasterEntity> _yearMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
        private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
        private readonly ILogger<WarrentNoticeDataProvider> _logger;

        public WarrentNoticeDataProvider(
            IReportDataRepository<PropertyEntity> propertyRepository,
            IReportDataRepository<WardEntity> wardRepository,
            IReportDataRepository<ZoneEntity> zoneRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeMasterRepository,
            IReportDataRepository<TransMastEntity> transRepository,
            IReportDataRepository<TaxPendingDetailsEntity> taxPendingRepository,
            IReportDataRepository<UserEntity> userRepository,
            IReportDataRepository<YearMasterEntity> yearMastRepository,
            IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
            IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
            ILogger<WarrentNoticeDataProvider> logger)
        {
            _propertyRepository = propertyRepository;
            _wardRepository = wardRepository;
            _zoneRepository = zoneRepository;
            _propertyTypeMasterRepository = propertyTypeMasterRepository;
            _transRepository = transRepository;
            _taxPendingRepository = taxPendingRepository;
            _userRepository = userRepository;
            _yearMastRepository = yearMastRepository;
            _ulbMasterRepository = ulbMasterRepository;
            _ReportRequestRepository = reportRequestRepository;
            _logger = logger;
        }

        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection, false),
        };

        public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(
            Guid reportRequestId,
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 100;

            var (rows, hasMore) = await BuildPageAsync(reportRequestId, parameters, (page - 1) * pageSize, pageSize, ct);
            return new ReportDataPage
            {
                Section = MainSection,
                Page = page,
                PageSize = pageSize,
                TotalCount = -1,
                HasMore = hasMore,
                Rows = rows,
            };
        }

        private static string? GetParam(Dictionary<string, string> parameters, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (parameters.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                    return val.Trim();
            }
            return null;
        }

        // Supports both "financeYear" and "financeyear" (case-insensitive key variants)
        private static short ParseFinanceYear(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("financeYear", out var financeYearStr))
                parameters.TryGetValue("financeyear", out financeYearStr);
            short.TryParse(financeYearStr, out var financeYear);
            return financeYear;
        }

        private IQueryable<YearMasterEntity> BaseQuery(short financeYear) =>
            _yearMastRepository.GetQueryable()
                // Accept both the year value (2026) and the legacy UI's
                // YearMaster Id (3002). Missing input still selects active year.
                .Where(b => financeYear == 0
                    ? b.IsActive
                    : b.Year == financeYear || b.Id == financeYear);

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Guid reportRequestId,
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            // --- Parse parameters matching BlankHearingFormatDataProvider ---
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

            parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
            fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText) ? null : fromPropertyNoText.Trim();

            parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
            toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText) ? null : toPropertyNoText.Trim();

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

            // financeYear supports both "financeYear" and "financeyear" keys
            var financeYear = ParseFinanceYear(parameters);

            parameters.TryGetValue("top_n", out var topNText);
            if (string.IsNullOrWhiteSpace(topNText))
                parameters.TryGetValue("topN", out topNText);
            int.TryParse(topNText, out var topN);

            // --- 1. Resolve Active Financial Year (same as BlankHearingFormat) ---
            var activeYear = await BaseQuery(financeYear)
                .Select(ym => new
                {
                    ym.Id,
                    ym.Year,
                    ym.YearCode,
                })
                .FirstOrDefaultAsync(ct);

            var activeYearId = activeYear?.Id ?? 0;

            // --- 2. Query Properties matching filters ---
            var query = from p in _propertyRepository.GetQueryable()
                        join w in _wardRepository.GetQueryable() on p.WardId equals w.Id into wj
                        from w in wj.DefaultIfEmpty()
                        join zm in _zoneRepository.GetQueryable() on w.ZoneId equals zm.Id into zmj
                        from zm in zmj.DefaultIfEmpty()
                        join pt in _propertyTypeMasterRepository.GetQueryable() on p.PropertyTypeId equals pt.Id into ptj
                        from pt in ptj.DefaultIfEmpty()
                        where p.IsActive && !p.MarkedForDeletion
                              && (ownerIds.Count == 0 || ownerIds.Contains(p.Id))
                              && (zoneId == 0 || w.ZoneId == zoneId)
                              && (wardId == 0 || p.WardId == wardId)
                              && (propertyNoText == null || p.PropertyNo == propertyNoText)
                              && (partitionNoText == null || p.PartitionNo == partitionNoText)
                              && (assessmentStatus == 0 || p.PropertyAssessmentStatusId == assessmentStatus)
                              && (string.IsNullOrEmpty(type) || pt.Type == type)
                              && (propertyTypeId == 0 || pt.Id == propertyTypeId)
                              && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)
                        select new
                        {
                            p.Id,
                            p.WardId,
                            WardNo = w.WardNo,
                            ZoneId = w.ZoneId,
                            ZoneNo = zm.ZoneNo,
                            ZoneDescription = zm.Description,
                            p.PropertyNo,
                            p.PartitionNo,
                            p.OwnerTitle,
                            p.OwnerName,
                            p.OwnerTitleEnglish,
                            p.OwnerNameEnglish,
                            p.OccupierTitle,
                            p.OccupierName,
                            p.OccupierTitleEnglish,
                            p.OccupierNameEnglish,
                            p.Address,
                            p.AddressEnglish,
                            p.PropertyTypeId,
                            PropertyType = pt.Type,
                            PropertyDescription = pt.PropertyDescription,
                        };

            // Apply the TransMast finance-year filter server-side only when the caller
            // explicitly supplies financeYear. Without it, properties must not be
            // excluded just because they have no transaction in the active year.
            if (financeYear != 0 && activeYearId > 0)
            {
                var propertyIdsWithTransaction = _transRepository.GetQueryable()
                    .Where(t => t.FinanceYearId == activeYearId)
                    .Select(t => t.PropertyId);

                query = query.Where(p => propertyIdsWithTransaction.Contains(p.Id));
            }

            // Apply range filtering, ordering and pagination in SQL, matching the
            // DocumentNotice provider. This avoids loading every matching property
            // before returning a single report page.
            var hasFromRange = int.TryParse(fromPropertyNoText, out var fromPropertyNo);
            var hasToRange = int.TryParse(toPropertyNoText, out var toPropertyNo);

            var queryWithNumericPropertyNo = query
                .Distinct()
                .Select(x => new
                {
                    Data = x,
                    NumericPropertyNo =
                        x.PropertyNo != null &&
                        x.PropertyNo.Trim() != "" &&
                        !EF.Functions.Like(x.PropertyNo.Trim(), "%[^0-9]%")
                            ? (int?)Convert.ToInt32(x.PropertyNo.Trim())
                            : null,
                });

            if (hasFromRange)
            {
                queryWithNumericPropertyNo = queryWithNumericPropertyNo.Where(x =>
                    x.NumericPropertyNo.HasValue &&
                    x.NumericPropertyNo.Value >= fromPropertyNo);
            }

            if (hasToRange)
            {
                queryWithNumericPropertyNo = queryWithNumericPropertyNo.Where(x =>
                    x.NumericPropertyNo.HasValue &&
                    x.NumericPropertyNo.Value <= toPropertyNo);
            }

            var orderedQuery = queryWithNumericPropertyNo
                .OrderBy(x => x.NumericPropertyNo)
                .ThenBy(x => x.Data.PartitionNo);

            var limitedQuery = topN > 0
                ? orderedQuery.Take(topN)
                : orderedQuery;

            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var properties = await limitedQuery
                .Skip(skip)
                .Take(takePlusOne)
                .Select(x => x.Data)
                .ToListAsync(ct);

            var hasMore = take != int.MaxValue && properties.Count > take;
            if (hasMore)
                properties = properties.Take(take).ToList();

            // --- 5a. Calculate TaxAmount and PendingAmount for each property ---
            var propertyIds = properties.Select(p => p.Id).ToList();

            // TransMast — SUM(TaxAmount) per PropertyId (IsActive = true)
            var currentTaxSums = await _transRepository.GetQueryable()
                .Where(tm => propertyIds.Contains(tm.PropertyId) && tm.FinanceYearId == activeYearId
                    && tm.IsActive && !tm.MarkedForDeletion)
                .GroupBy(tm => tm.PropertyId)
                .Select(g => new { PropertyId = g.Key, Total = g.Sum(tm => tm.TaxAmount) })
                .ToListAsync(ct);
            var currentTaxMap = currentTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

            // TaxPendingDetails — SUM(PendingAmount) per PropertyId (IsActive = true)
            var pendingTaxSums = await _taxPendingRepository.GetQueryable()
                .Where(tp => propertyIds.Contains(tp.PropertyId) && tp.IsActive && !tp.MarkedForDeletion && !tp.PendingFixed)
                .GroupBy(tp => tp.PropertyId)
                .Select(g => new { PropertyId = g.Key, Total = g.Sum(tp => tp.PendingAmount) ?? 0m })
                .ToListAsync(ct);
            var pendingTaxMap = pendingTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

            // --- 6. Fetch requested User info ---
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

            // --- 7. Fetch the configured Thane ULB row; do not fall back to another active corporation. ---
            var ulb = await _ulbMasterRepository.GetQueryable()
                .Where(u => u.IsActive && u.UlbCode == "TH001")
                .OrderBy(u => u.Id)
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

            // --- 8. Build output rows ---
            var rows = properties.Select(property =>
            {
                // Get TaxAmount and PendingAmount for this property
                var taxAmount = currentTaxMap.GetValueOrDefault(property.Id, 0m);
                var pendingAmount = pendingTaxMap.GetValueOrDefault(property.Id, 0m);
                var totalDemand = taxAmount + pendingAmount;

                return new Dictionary<string, object?>
                {
                    // Active year fields
                    ["activeYearId"] = activeYear?.Id,
                    ["year"] = activeYear?.Year,
                    ["yearCode"] = activeYear?.YearCode,
                    // Property fields
                    ["propertyId"] = property.Id,
                    ["wardId"] = property.WardId,
                    ["wardNo"] = property.WardNo,
                    ["zoneId"] = property.ZoneId,
                    ["zoneNo"] = property.ZoneNo,
                    ["zoneDescription"] = property.ZoneDescription,
                    ["propertyNo"] = property.PropertyNo,
                    ["partitionNo"] = property.PartitionNo,
                    ["ownerTitle"] = property.OwnerTitle,
                    ["ownerName"] = property.OwnerName,
                    ["ownerTitleEnglish"] = property.OwnerTitleEnglish,
                    ["ownerNameEnglish"] = property.OwnerNameEnglish,
                    ["occupierTitle"] = property.OccupierTitle,
                    ["occupierName"] = property.OccupierName,
                    ["occupierTitleEnglish"] = property.OccupierTitleEnglish,
                    ["occupierNameEnglish"] = property.OccupierNameEnglish,
                    ["address"] = property.Address,
                    ["propertyTypeId"] = property.PropertyTypeId,
                    ["propertyType"] = property.PropertyType,
                    ["propertyDescription"] = property.PropertyDescription,
                    // Tax amount fields
                    ["taxAmount"] = taxAmount,
                    ["pendingAmount"] = pendingAmount,
                    ["totalDemand"] = totalDemand,
                    // User fields
                    ["userId"] = user?.Id,
                    ["userName"] = user?.UserName,
                    ["userCode"] = user?.UserCode,
                    ["userEmail"] = user?.Email,
                    ["userMobileNo"] = user?.MobileNo,
                    // ULB Master fields
                    ["ulbCode"] = ulb?.UlbCode,
                    ["ulbName"] = ulb?.UlbName,
                    ["ulbNameLocal"] = ulb?.UlbNameLocal,
                    ["ulbLogo"] = ulb?.UlbLogo,
                    ["ulbEmailId"] = ulb?.EmailId,
                    ["ulbMobileNo"] = ulb?.MobileNo,
                    ["ulbAlternateMobileNo"] = ulb?.AlternateMobileNo,
                    ["ulbWebsiteUrl"] = ulb?.WebsiteUrl,
                    ["ulbAddress"] = ulb?.UlbAddress,
                    ["ulbState"] = ulb?.State,
                    ["ulbDistrict"] = ulb?.District,
                    ["ulbPinCode"] = ulb?.PinCode,
                    // Exact aliases used by WarrentNotice.xsd / Crystal template.
                    ["emailId"] = ulb?.EmailId,
                    ["mobileNo"] = ulb?.MobileNo,
                    ["mobileNumber"] = ulb?.MobileNo,
                    ["alternateMobileNo"] = ulb?.AlternateMobileNo,
                    ["websiteUrl"] = ulb?.WebsiteUrl,
                    ["state"] = ulb?.State,
                    ["district"] = ulb?.District,
                    ["pinCode"] = ulb?.PinCode,
                    ["wardPropertyPartitionNo"] = $"{property.WardNo}-{property.PropertyNo}-{property.PartitionNo}",
                };
            }).ToList();

            // Runtime diagnostic for Crystal Reports schema/value issues. Keep this scoped to the
            // first page so a large report does not produce one informational log per page.
            if (skip == 0)
            {
                var firstRow = rows.FirstOrDefault();
                object? firstWardNo = null;
                var hasWardNoField = firstRow?.TryGetValue("wardNo", out firstWardNo) == true;
                var missingWardNoCount = properties.Count(p => string.IsNullOrWhiteSpace(p.WardNo));

                _logger.LogInformation(
                    "WarrentNotice runtime data check for request {ReportRequestId}: RowCount={RowCount}, " +
                    "HasWardNoField={HasWardNoField}, FirstWardNo={FirstWardNo}, MissingWardNoCount={MissingWardNoCount}.",
                    reportRequestId,
                    rows.Count,
                    hasWardNoField,
                    hasWardNoField ? firstWardNo : null,
                    missingWardNoCount);

                if (missingWardNoCount > 0)
                {
                    var sampleMissingWard = properties.First(p => string.IsNullOrWhiteSpace(p.WardNo));
                    _logger.LogWarning(
                        "WarrentNotice request {ReportRequestId} has {MissingWardNoCount} row(s) with a blank wardNo. " +
                        "Sample PropertyId={PropertyId}, WardId={WardId}; verify the Ward master join.",
                        reportRequestId,
                        missingWardNoCount,
                        sampleMissingWard.Id,
                        sampleMissingWard.WardId);
                }
            }

            return (rows.Cast<object>().ToList(), hasMore);
        }
    }
}
