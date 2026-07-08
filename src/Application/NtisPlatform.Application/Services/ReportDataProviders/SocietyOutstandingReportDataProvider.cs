using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// SocietyOutstandingReport data provider.
    ///
    /// Accepts parameters:
    ///   wardId         — mandatory: PTIS.PropertyMast.WardId
    ///   fromPropertyNo — mandatory: lower bound of PropertyNo range
    ///   toPropertyNo   — mandatory: upper bound of PropertyNo range
    ///   userId         — mandatory: CORE.UserMaster lookup
    ///   partitionNo    — optional:  if provided, further filters by PartitionNo
    ///
    /// Returns one row per matched property, each row containing:
    ///   — Core property fields (propertyId, propertyNo, wardNo, partitionNo, owner, address …)
    ///   — Society details (wingName, societyName, societyAddress)
    ///   — Old property data (oldWardNo, oldPropertyNo, oldPartitionNo)
    ///   — User + ULB master fields
    ///   — totalCurrentTaxAmount  = SUM(TransMast.TaxAmount)        WHERE PropertyId = … AND MarkedForDeletion = false
    ///   — totalPendingTaxAmount  = SUM(TaxPendingDetails.PendingAmount) WHERE PropertyId = … AND MarkedForDeletion = false
    ///
    /// Section discovery is static (no query runs during authenticate).
    /// </summary>
    public class SocietyOutstandingReportDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";

        public string ProviderCode => "SocietyOutstandingReportDataProvider";

        private readonly IReportDataRepository<PropertyEntity>           _propertyRepository;
        private readonly IReportDataRepository<WardEntity>               _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity>     _societyRepository;
        private readonly IReportDataRepository<PropertyMastOldEntity>    _propertyMastOldRepository;
        private readonly IReportDataRepository<TransMastEntity>          _transmastRepository;
        private readonly IReportDataRepository<TaxPendingDetailsEntity>  _taxPendingRepository;
        private readonly IReportDataRepository<ULBMasterEntity>          _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity>               _userRepository;

        public SocietyOutstandingReportDataProvider(
            IReportDataRepository<PropertyEntity>           propertyRepository,
            IReportDataRepository<WardEntity>               wardRepository,
            IReportDataRepository<SocietyDetailsEntity>     societyRepository,
            IReportDataRepository<PropertyMastOldEntity>    propertyMastOldRepository,
            IReportDataRepository<TransMastEntity>          transmastRepository,
            IReportDataRepository<TaxPendingDetailsEntity>  taxPendingRepository,
            IReportDataRepository<ULBMasterEntity>          ulbMasterRepository,
            IReportDataRepository<UserEntity>               userRepository)
        {
            _propertyRepository        = propertyRepository;
            _wardRepository            = wardRepository;
            _societyRepository         = societyRepository;
            _propertyMastOldRepository = propertyMastOldRepository;
            _transmastRepository       = transmastRepository;
            _taxPendingRepository      = taxPendingRepository;
            _ulbMasterRepository       = ulbMasterRepository;
            _userRepository            = userRepository;
        }

        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection, false),
        };

        public async Task<object> GetDataAsync(
            Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var (rows, _) = await BuildPageAsync(parameters, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            if (page     < 1)  page     = 1;
            if (pageSize <= 0) pageSize = 100;

            var (rows, hasMore) = await BuildPageAsync(parameters, (page - 1) * pageSize, pageSize, ct);
            return new ReportDataPage
            {
                Section    = MainSection,
                Page       = page,
                PageSize   = pageSize,
                TotalCount = -1,
                HasMore    = hasMore,
                Rows       = rows,
            };
        }

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            // --- Parse parameters ---
            parameters.TryGetValue("wardId",         out var wardIdStr);
            parameters.TryGetValue("fromPropertyNo", out var fromPropertyNo);
            parameters.TryGetValue("toPropertyNo",   out var toPropertyNo);
            parameters.TryGetValue("userId",         out var userIdStr);
            parameters.TryGetValue("partitionNo",    out var partitionNo); // optional

            int.TryParse(wardIdStr,  out var wardId);
            int.TryParse(userIdStr,  out var userId);

            var rows = await BuildRowsAsync(wardId, fromPropertyNo, toPropertyNo, partitionNo, userId, ct);

            // Apply skip/take.
            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged       = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore     = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            return (paged, hasMore);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Core builder — returns one row per matching property.
        //
        // SQL equivalent for property filter:
        //   SELECT Id, PropertyNo, WardId, PartitionNo, ...
        //   FROM PTIS.PropertyMast
        //   WHERE WardId          = @wardId
        //     AND PropertyNo     >= @fromPropertyNo
        //     AND PropertyNo     <= @toPropertyNo
        //     AND (@partitionNo IS NULL OR PartitionNo = @partitionNo)
        //   ORDER BY PropertyNo, PartitionNo
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<List<object>> BuildRowsAsync(
            int wardId, string? fromPropertyNo, string? toPropertyNo,
            string? partitionNo, int userId, CancellationToken ct)
        {
            // ── 1. Fetch matching properties ──────────────────────────────────────
            var propertyQuery = _propertyRepository.GetQueryable()
                .Where(p => p.IsActive && !p.MarkedForDeletion
                          && p.WardId == wardId
                         && (fromPropertyNo == null || string.Compare(p.PropertyNo, fromPropertyNo) >= 0)
                         && (toPropertyNo   == null || string.Compare(p.PropertyNo, toPropertyNo)   <= 0));

            // PartitionNo filter is optional
            if (!string.IsNullOrWhiteSpace(partitionNo))
                propertyQuery = propertyQuery.Where(p => p.PartitionNo == partitionNo);

            var properties = await propertyQuery
                .OrderBy(p => p.PropertyNo)
                .ThenBy(p => p.PartitionNo)
                .Select(p => new
                {
                    p.Id,
                    p.PropertyNo,
                    p.WardId,
                    p.PartitionNo,
                    p.UPICId,
                    p.SubZoneNo,
                    p.MobileNo,
                    p.OwnerTitle,
                    p.OccupierTitle,
                    p.OwnerName,
                    p.OccupierName,
                    p.Address,
                    p.FlatOrShopNo,
                    p.FlatOrShopName,
                    p.PropertyMastOldId,
                })
                .ToListAsync(ct);

            if (!properties.Any())
                return new List<object>();

            // ── 2. Shared lookups (run once for the whole batch) ──────────────────

            // 2a. Ward — get WardNo for the given wardId
            var wardNo = await _wardRepository.GetQueryable()
                .Where(w => w.Id == wardId)
                .Select(w => w.WardNo)
                .FirstOrDefaultAsync(ct);

            // 2b. ULB Master (single row)
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

            // 2c. User
            var user = await _userRepository.GetQueryable()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.UserCode,
                    u.Email,
                    u.MobileNo,
                })
                .FirstOrDefaultAsync(ct);

            // ── 3. Collect all property IDs for batch queries ─────────────────────
            var propertyIds = properties.Select(p => p.Id).ToList();

            // 3a. Society details — keyed by PropertyId (int? in SocietyDetailsEntity)
            //     A property can have multiple wing rows; take the first per PropertyId.
            var societies = await _societyRepository.GetQueryable()
                .Where(sd => sd.PropertyId.HasValue && propertyIds.Contains(sd.PropertyId.Value))
                .Select(sd => new
                {
                    sd.PropertyId,
                    sd.WingName,
                    sd.SocietyName,
                    sd.SocietyAddress,
                })
                .ToListAsync(ct);
            // GroupBy to handle duplicate PropertyId rows (multiple wings per property)
            var societyMap = societies
                .GroupBy(s => s.PropertyId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            // 3b. PropertyMastOld — keyed by Id (matched via property.PropertyMastOldId)
            var oldIds = properties
                .Where(p => p.PropertyMastOldId.HasValue)
                .Select(p => p.PropertyMastOldId!.Value)
                .Distinct()
                .ToList();

            var propertyOlds = oldIds.Any()
                ? await _propertyMastOldRepository.GetQueryable()
                    .Where(o => oldIds.Contains(o.Id))
                    .Select(o => new
                    {
                        o.Id,
                        o.OldWardNo,
                        o.OldPropertyNo,
                        o.OldPartitionNo,
                    })
                    .ToListAsync(ct)
                : new List<dynamic>() as dynamic;
            var propertyOldMap = oldIds.Any()
                ? ((IEnumerable<dynamic>)propertyOlds).ToDictionary(o => (int)o.Id)
                : new Dictionary<int, dynamic>();

            // 3c. TransMast — SUM(TaxAmount) per PropertyId (IsActive = true)
            //     SQL: SELECT PropertyId, SUM(TaxAmount) FROM PTIS.TransMast
            //          WHERE PropertyId IN (@ids) AND IsActive = 1 GROUP BY PropertyId
            var currentTaxSums = await _transmastRepository.GetQueryable()
                .Where(tm => propertyIds.Contains(tm.PropertyId) && tm.IsActive)
                .GroupBy(tm => tm.PropertyId)
                .Select(g => new { PropertyId = g.Key, Total = g.Sum(tm => tm.TaxAmount) })
                .ToListAsync(ct);
            var currentTaxMap = currentTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

            // 3d. TaxPendingDetails — SUM(PendingAmount) per PropertyId (IsActive = true)
            //     SQL: SELECT PropertyId, SUM(PendingAmount) FROM PTIS.TaxPendingDetails
            //          WHERE PropertyId IN (@ids) AND IsActive = 1 GROUP BY PropertyId
            var pendingTaxSums = await _taxPendingRepository.GetQueryable()
                 .Where(tp => propertyIds.Contains(tp.PropertyId) && tp.IsActive && !tp.MarkedForDeletion)
                .GroupBy(tp => tp.PropertyId)
                .Select(g => new { PropertyId = g.Key, Total = g.Sum(tp => tp.PendingAmount) })
                .ToListAsync(ct);
            var pendingTaxMap = pendingTaxSums.ToDictionary(x => x.PropertyId, x => x.Total);

            // ── 4. Assemble one row per property ──────────────────────────────────
            var rows = new List<object>();

            foreach (var property in properties)
            {
                societyMap.TryGetValue(property.Id, out var society);
                propertyOldMap.TryGetValue(property.PropertyMastOldId ?? -1, out var propertyOld);
                currentTaxMap.TryGetValue(property.Id, out var totalCurrentTax);
                pendingTaxMap.TryGetValue(property.Id, out var totalPendingTax);

                var row = new Dictionary<string, object?>
                {
                    // Property fields
                    ["propertyId"]           = property.Id,
                    ["propertyNo"]           = property.PropertyNo,
                    ["wardId"]               = property.WardId,
                    ["wardNo"]               = wardNo,
                    ["partitionNo"]          = property.PartitionNo,
                    ["upicId"]               = property.UPICId,
                    ["subZoneNo"]            = property.SubZoneNo,
                    ["mobileNo"]             = property.MobileNo,
                    ["ownerTitle"]           = property.OwnerTitle,
                    ["occupierTitle"]        = property.OccupierTitle,
                    ["ownerName"]            = property.OwnerName,
                    ["occupierName"]         = property.OccupierName,
                    ["address"]              = property.Address,
                    ["flatOrShopNo"]         = property.FlatOrShopNo,
                    ["flatOrShopName"]       = property.FlatOrShopName,
                    // Society details
                    ["wingName"]             = society?.WingName,
                    ["societyName"]          = society?.SocietyName,
                    ["societyAddress"]       = society?.SocietyAddress,
                    // Old property data (PTIS.PropertyMastOld)
                    ["oldWardNo"]            = propertyOld?.OldWardNo,
                    ["oldPropertyNo"]        = propertyOld?.OldPropertyNo,
                    ["oldPartitionNo"]       = propertyOld?.OldPartitionNo,
                    // Aggregated tax amounts
                    ["totalCurrentTaxAmount"] = totalCurrentTax,
                    ["totalPendingTaxAmount"] = totalPendingTax,
                    // User Master fields (CORE.UserMaster)
                    ["userId"]               = user?.Id,
                    ["userName"]             = user?.UserName,
                    ["firstName"]            = user?.FirstName,
                    ["middleName"]           = user?.MiddleName,
                    ["lastName"]             = user?.LastName,
                    ["userCode"]             = user?.UserCode,
                    ["userEmail"]            = user?.Email,
                    ["userMobileNo"]         = user?.MobileNo,
                    // ULB Master fields (CORE.UlbMaster)
                    ["ulbCode"]              = ulb?.UlbCode,
                    ["ulbName"]              = ulb?.UlbName,
                    ["ulbNameLocal"]         = ulb?.UlbNameLocal,
                    ["ulbLogo"]              = ulb?.UlbLogo,
                    ["ulbEmailId"]           = ulb?.EmailId,
                    ["ulbMobileNo"]          = ulb?.MobileNo,
                    ["ulbAlternateMobileNo"] = ulb?.AlternateMobileNo,
                    ["ulbWebsiteUrl"]        = ulb?.WebsiteUrl,
                    ["ulbAddress"]           = ulb?.UlbAddress,
                    ["ulbState"]             = ulb?.State,
                    ["ulbDistrict"]          = ulb?.District,
                    ["ulbPinCode"]           = ulb?.PinCode,
                };

                rows.Add(row);
            }

            return rows;
        }
    }
}
