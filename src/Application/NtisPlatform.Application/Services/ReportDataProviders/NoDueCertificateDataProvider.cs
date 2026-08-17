using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// NoDueCertificate report data provider.
    /// 
    /// Produces one row per property.
    /// Parameters: propertyId (int or comma-separated list), userId (int)
    /// </summary>
    public class NoDueCertificateDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";

        public string ProviderCode => "NoDueCertificateDataProvider";

        private readonly IReportDataRepository<PropertyEntity>   _propertyRepository;
        private readonly IReportDataRepository<WardEntity>        _wardRepository;
        private readonly IReportDataRepository<UserEntity>        _userRepository;
        private readonly IReportDataRepository<YearMasterEntity>  _yearMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity>   _ulbMasterRepository;

        public NoDueCertificateDataProvider(
            IReportDataRepository<PropertyEntity>   propertyRepository,
            IReportDataRepository<WardEntity>       wardRepository,
            IReportDataRepository<UserEntity>       userRepository,
            IReportDataRepository<YearMasterEntity> yearMastRepository,
            IReportDataRepository<ULBMasterEntity>  ulbMasterRepository)
        {
            _propertyRepository  = propertyRepository;
            _wardRepository      = wardRepository;
            _userRepository      = userRepository;
            _yearMastRepository  = yearMastRepository;
            _ulbMasterRepository = ulbMasterRepository;
        }
                                                                                                                                                                                                         
        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection, false),
        };

        public async Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var (rows, _) = await BuildPageAsync(parameters, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(Guid reportRequestId,
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 100;

            var (rows, hasMore) = await BuildPageAsync(parameters, (page - 1) * pageSize, pageSize, ct);
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

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            // --- Parse parameters ---
            // propertyId accepts a single value OR comma-separated list: "101,202,303"
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId", out var userIdStr);

            // Split on commas, parse each token, deduplicate, drop invalid entries.
            var propertyIds = (propertyIdStr ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            int.TryParse(userIdStr, out var userId);

            // --- 1. Active year: select Year, YearCode from [CORE].[YearMaster] where IsActive = 1 ---
            var activeYear = await _yearMastRepository.GetQueryable()
                .Where(ym => ym.IsActive)
                .Select(ym => new
                {
                    ym.Id,
                    ym.Year,
                    ym.YearCode,
                })
                .FirstOrDefaultAsync(ct);

            // --- 2. All properties in one batch IN query ---
            var properties = await _propertyRepository.GetQueryable()
                .Where(p => propertyIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
                .Select(p => new
                {
                    p.Id,
                    p.WardId,
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

                })
                .ToListAsync(ct);

            // --- 3. Unique ward IDs — resolve WardNo one-by-one (avoids nullable-int JOIN crash) ---
            var uniqueWardIds = properties
                .Select(p => p.WardId)
                .Where(wid => wid > 0)
                .Distinct()
                .ToList();

            var wardMap = new Dictionary<int, string?>();
            foreach (var wid in uniqueWardIds)
            {
                var wardNo = await _wardRepository.GetQueryable()
                    .Where(w => w.Id == wid)
                    .Select(w => w.WardNo)
                    .FirstOrDefaultAsync(ct);
                wardMap[wid] = wardNo;
            }

            // --- 4. User: select UserName, FirstName, MiddleName, LastName from [CORE].[UserMaster] where ID = @userId ---
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

            // --- 5. ULB: select UlbCode, UlbName, ... from [CORE].[UlbMaster] ---
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

            // --- Build output rows: one row per property ---
            var allRows = new List<Dictionary<string, object?>>();

            foreach (var property in properties)
            {
                // Resolve ward number from the ward map
                string? wardNo = null;
                if (property.WardId > 0 && wardMap.ContainsKey(property.WardId))
                {
                    wardNo = wardMap[property.WardId];
                }

                allRows.Add(new Dictionary<string, object?>
                {
                    // Active year fields
                    ["activeYearId"]              = activeYear?.Id,
                    ["year"]                      = activeYear?.Year,
                    ["yearCode"]                  = activeYear?.YearCode,
                    ["propertyId"]                = property.Id,
                    ["wardId"]                    = property.WardId,
                    ["wardNo"]                    = wardNo,           // from PTIS.WardMaster
                    ["propertyNo"]                = property.PropertyNo,
                    ["partitionNo"]               = property.PartitionNo,
                    ["ownerTitle"]                = property.OwnerTitle,
                    ["ownerName"]                 = property.OwnerName,
                    ["ownerTitleEnglish"]         = property.OwnerTitleEnglish,
                    ["ownerNameEnglish"]          = property.OwnerNameEnglish,
                    ["occupierTitle"]             = property.OccupierTitle,
                    ["occupierName"]              = property.OccupierName,
                    ["occupierTitleEnglish"]      = property.OccupierTitleEnglish,
                    ["occupierNameEnglish"]       = property.OccupierNameEnglish,
                    ["userId"]                    = user?.Id,
                    ["userName"]                  = user?.UserName,
                    ["firstName"]                 = user?.FirstName,
                    ["middleName"]                = user?.MiddleName,
                    ["lastName"]                  = user?.LastName,
                    ["userCode"]                  = user?.UserCode,
                    ["userEmail"]                 = user?.Email,
                    ["userMobileNo"]              = user?.MobileNo,
                    // ULB Master fields 
                    ["ulbCode"]                   = ulb?.UlbCode,
                    ["ulbName"]                   = ulb?.UlbName,
                    ["ulbNameLocal"]              = ulb?.UlbNameLocal,
                    ["ulbLogo"]                   = ulb?.UlbLogo,
                    ["ulbEmailId"]                = ulb?.EmailId,
                    ["ulbMobileNo"]               = ulb?.MobileNo,
                    ["ulbAlternateMobileNo"]      = ulb?.AlternateMobileNo,
                    ["ulbWebsiteUrl"]             = ulb?.WebsiteUrl,
                    ["ulbAddress"]                = ulb?.UlbAddress,
                    ["ulbState"]                  = ulb?.State,
                    ["ulbDistrict"]               = ulb?.District,
                    ["ulbPinCode"]                = ulb?.PinCode,
                });
            }

            // Apply skip/take so the paged overload works correctly.
            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged = allRows.Skip(skip).Take(takePlusOne).ToList();

            var hasMore = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            var rows = paged.Cast<object>().ToList();
            return (rows, hasMore);
        }
    }
}
