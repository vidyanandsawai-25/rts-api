using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    
    public class RentedNoticeDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";

        public string ProviderCode => "RentedNoticeDataProvider";

        private readonly IReportDataRepository<PropertyEntity>           _propertyRepository;
        private readonly IReportDataRepository<WardEntity>               _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity>     _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity>          _typeOfUseRepository;
        private readonly IReportDataRepository<PropertyMastOldEntity>    _propertyMastOldRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;
        private readonly IReportDataRepository<TransMastEntity>          _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity>          _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity>          _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity>               _userRepository;

        public RentedNoticeDataProvider(
            IReportDataRepository<PropertyEntity>           propertyRepository,
            IReportDataRepository<WardEntity>               wardRepository,
            IReportDataRepository<SocietyDetailsEntity>     societyRepository,
            IReportDataRepository<TypeOfUseEntity>          typeOfUseRepository,
            IReportDataRepository<PropertyMastOldEntity>    propertyMastOldRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository,
            IReportDataRepository<TransMastEntity>          transmastRepository,
            IReportDataRepository<TaxMasterEntity>          taxMastRepository,
            IReportDataRepository<ULBMasterEntity>          ulbMasterRepository,
            IReportDataRepository<UserEntity>               userRepository)
        {
            _propertyRepository        = propertyRepository;
            _wardRepository            = wardRepository;
            _societyRepository         = societyRepository;
            _typeOfUseRepository       = typeOfUseRepository;
            _propertyMastOldRepository = propertyMastOldRepository;
            _propertyTypeRepository    = propertyTypeRepository;
            _transmastRepository       = transmastRepository;
            _taxMastRepository         = taxMastRepository;
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
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId",     out var userIdStr);
            int.TryParse(propertyIdStr, out var propertyId);
            int.TryParse(userIdStr,     out var userId);

            var rows = await BuildMainRowAsync(propertyId, userId, ct);

            // Apply skip/take.
            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged       = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore     = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            return (paged, hasMore);
        }

        private async Task<List<object>> BuildMainRowAsync(int propertyId, int userId, CancellationToken ct)
        {
            // 1a. Property
            var property = await _propertyRepository.GetQueryable()
                .Where(p => p.Id == propertyId)
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
                    p.PropertyTypeId,
                    p.PropertyMastOldId,
                })
                .FirstOrDefaultAsync(ct);

            if (property == null)
                return new List<object>();

            // 1b. Ward: JOIN PTIS.WardMaster ON WardId = Id to get WardNo
            string? wardNo = null;
            if (property.WardId is int wid and > 0)
            {
                wardNo = await _wardRepository.GetQueryable()
                    .Where(w => w.Id == wid)
                    .Select(w => w.WardNo)
                    .FirstOrDefaultAsync(ct);
            }

            // 1b. Society details (LEFT JOIN on PropertyId)
            var society = await _societyRepository.GetQueryable()
                .Where(sd => sd.PropertyId == propertyId)
                .Select(sd => new
                {
                    sd.WingId,
                    sd.WingName,
                    sd.SocietyName,
                    sd.SocietyAddress,
                })
                .FirstOrDefaultAsync(ct);

            // 1c. Type-of-use (LEFT JOIN on PM.PropertyTypeId = TUM.Id)
            var typeOfUse = property.PropertyTypeId.HasValue
                ? await _typeOfUseRepository.GetQueryable()
                    .Where(t => t.Id == property.PropertyTypeId.Value)
                    .Select(t => new
                    {
                        t.Description,
                        t.TypeOfUseCode,
                    })
                    .FirstOrDefaultAsync(ct)
                : null;

            // 1d. PropertyMastOld (LEFT JOIN on PM.PropertyMastOldId = PMO.Id)
            //     Returns OldWardNo, OldPropertyNo, OldPartitionNo
            var propertyOld = property.PropertyMastOldId.HasValue
                ? await _propertyMastOldRepository.GetQueryable()
                    .Where(o => o.Id == property.PropertyMastOldId.Value)
                    .Select(o => new
                    {
                        o.OldWardNo,
                        o.OldPropertyNo,
                        o.OldPartitionNo,
                    })
                    .FirstOrDefaultAsync(ct)
                : null;

            // 1e. PropertyTypeMaster (LEFT JOIN on PM.PropertyTypeId = PTM.Id)
            //     Returns PropertyDescription, Type, PartType
            var propertyType = property.PropertyTypeId.HasValue
                ? await _propertyTypeRepository.GetQueryable()
                    .Where(pt => pt.Id == property.PropertyTypeId.Value)
                    .Select(pt => new
                    {
                        pt.PropertyDescription,
                        pt.Type,
                        pt.PartType,
                    })
                    .FirstOrDefaultAsync(ct)
                : null;

            // 1f. ULB Master — SELECT from [CORE].[UlbMaster] (first/only row)
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

            // 1g. User: select from [CORE].[UserMaster] where Id = @userId
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

            // 1h. TransMast pivot
            var taxRows = await (
                from tm  in _transmastRepository.GetQueryable().Where(t => t.PropertyId == propertyId)
                join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
                orderby tam.DisplayOrder
                select new
                {
                    tam.TaxCode,
                    tam.TaxName,
                    tm.RVorCV,
                    tm.RVorCVValue,
                    tm.TaxAmount,
                }
            ).ToListAsync(ct);

            var row = new Dictionary<string, object?>
            {
                // Property fields
                ["propertyId"]               = property.Id,
                ["propertyNo"]               = property.PropertyNo,
                ["wardId"]                   = property.WardId,
                ["wardNo"]                   = wardNo,
                ["partitionNo"]              = property.PartitionNo,
                ["upicId"]                   = property.UPICId,
                ["subZoneNo"]                = property.SubZoneNo,
                ["mobileNo"]                 = property.MobileNo,
                ["ownerTitle"]               = property.OwnerTitle,
                ["occupierTitle"]            = property.OccupierTitle,
                ["ownerName"]                = property.OwnerName,
                ["occupierName"]             = property.OccupierName,
                ["address"]                  = property.Address,
                ["flatOrShopNo"]             = property.FlatOrShopNo,
                ["flatOrShopName"]           = property.FlatOrShopName,
                // Society details
                ["wingId"]                   = society?.WingId,
                ["wingName"]                 = society?.WingName,
                ["societyName"]              = society?.SocietyName,
                ["societyAddress"]           = society?.SocietyAddress,
                // Type-of-use (TypeOfUseMaster)
                ["typeOfUseDesc"]            = typeOfUse?.Description,
                ["typeOfUseCode"]            = typeOfUse?.TypeOfUseCode,
                // Old property data (PTIS.PropertyMastOld)
                ["oldWardNo"]                = propertyOld?.OldWardNo,
                ["oldPropertyNo"]            = propertyOld?.OldPropertyNo,
                ["oldPartitionNo"]           = propertyOld?.OldPartitionNo,
                // Property type master (PTIS.PropertyTypeMaster)
                ["propertyDescription"]      = propertyType?.PropertyDescription,
                ["propertyType"]             = propertyType?.Type,
                ["propertyPartType"]         = propertyType?.PartType,
                // User Master fields (CORE.UserMaster)
                ["userId"]                   = user?.Id,
                ["userName"]                 = user?.UserName,
                ["firstName"]                = user?.FirstName,
                ["middleName"]               = user?.MiddleName,
                ["lastName"]                 = user?.LastName,
                ["userCode"]                 = user?.UserCode,
                ["userEmail"]                = user?.Email,
                ["userMobileNo"]             = user?.MobileNo,
                // ULB Master fields (CORE.UlbMaster)
                ["ulbCode"]                  = ulb?.UlbCode,
                ["ulbName"]                  = ulb?.UlbName,
                ["ulbNameLocal"]             = ulb?.UlbNameLocal,
                ["ulbLogo"]                  = ulb?.UlbLogo,
                ["ulbEmailId"]               = ulb?.EmailId,
                ["ulbMobileNo"]              = ulb?.MobileNo,
                ["ulbAlternateMobileNo"]     = ulb?.AlternateMobileNo,
                ["ulbWebsiteUrl"]            = ulb?.WebsiteUrl,
                ["ulbAddress"]               = ulb?.UlbAddress,
                ["ulbState"]                 = ulb?.State,
                ["ulbDistrict"]              = ulb?.District,
                ["ulbPinCode"]               = ulb?.PinCode,
            };

            // Pivot TransMast rows into dynamic columns on the same main row.
            foreach (var tax in taxRows)
            {
                var safeCode = (tax.TaxCode?.Trim().Length > 0
                                    ? tax.TaxCode
                                    : tax.TaxName ?? "UNKNOWN")
                               .Replace(' ', '_');

                row[$"Transmast_{safeCode}"]   = tax.TaxAmount;
                row[$"RVorCV_{safeCode}"]      = tax.RVorCV;
                row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
            }

            return new List<object> { row };
        }
    }
}
