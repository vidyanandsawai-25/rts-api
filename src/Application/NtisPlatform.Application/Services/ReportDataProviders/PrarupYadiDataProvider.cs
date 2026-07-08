using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// PrarupYadi report data provider.
    ///
    /// Produces three logical sections per call:
    ///   "main"            — one row: all RentedNotice fields
    ///                       (property + ward + society + typeOfUse + propertyOld +
    ///                        propertyTypeMaster + ULB + user + pivoted TransMast columns)
    ///   "propertyDetails" — one row per PTIS.PropertyDetails floor entry
    ///   "taxDetails"      — one pivoted row of TransMast amounts keyed by TaxCode
    ///
    /// Parameters: propertyId (int), userId (int)
    /// Section discovery is static (no query runs during authenticate).
    /// </summary>
    public class PrarupYadiDataProvider : IPagedReportDataProvider
    {
        public const string MainSection            = "main";
        public const string PropertyDetailsSection = "propertyDetails";
        public const string TaxDetailsSection      = "taxDetails";

        public string ProviderCode => "PrarupYadiDataProvider";

        private readonly IReportDataRepository<PropertyEntity>           _propertyRepository;
        private readonly IReportDataRepository<WardEntity>               _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity>     _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity>          _typeOfUseRepository;
        private readonly IReportDataRepository<PropertyMastOldEntity>    _propertyMastOldRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;
        private readonly IReportDataRepository<PropertyDetailsEntity>    _propertyDetailsRepository;
        private readonly IReportDataRepository<TransMastEntity>          _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity>          _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity>          _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity>               _userRepository;

        public PrarupYadiDataProvider(
            IReportDataRepository<PropertyEntity>           propertyRepository,
            IReportDataRepository<WardEntity>               wardRepository,
            IReportDataRepository<SocietyDetailsEntity>     societyRepository,
            IReportDataRepository<TypeOfUseEntity>          typeOfUseRepository,
            IReportDataRepository<PropertyMastOldEntity>    propertyMastOldRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository,
            IReportDataRepository<PropertyDetailsEntity>    propertyDetailsRepository,
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
            _propertyDetailsRepository = propertyDetailsRepository;
            _transmastRepository       = transmastRepository;
            _taxMastRepository         = taxMastRepository;
            _ulbMasterRepository       = ulbMasterRepository;
            _userRepository            = userRepository;
        }

        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection,            false),
            new ReportSectionDescriptor(PropertyDetailsSection, false),
            new ReportSectionDescriptor(TaxDetailsSection,      false),
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

            // --- Parse parameters ---
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId",     out var userIdStr);
            int.TryParse(propertyIdStr, out var propertyId);
            int.TryParse(userIdStr,     out var userId);

            List<object> rows;
            switch (section)
            {
                case PropertyDetailsSection:
                    rows = await BuildPropertyDetailsRowsAsync(propertyId, ct);
                    break;

                case TaxDetailsSection:
                    rows = await BuildTaxDetailsRowsAsync(propertyId, ct);
                    break;

                default: // MainSection
                    rows = await BuildMainRowAsync(propertyId, userId, ct);
                    break;
            }

            var skip        = (page - 1) * pageSize;
            var takePlusOne = pageSize + 1;
            var paged       = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore     = paged.Count > pageSize;
            if (hasMore) paged = paged.Take(pageSize).ToList();

            return new ReportDataPage
            {
                Section    = section,
                Page       = page,
                PageSize   = pageSize,
                TotalCount = -1,
                HasMore    = hasMore,
                Rows       = paged,
            };
        }

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId",     out var userIdStr);
            int.TryParse(propertyIdStr, out var propertyId);
            int.TryParse(userIdStr,     out var userId);

            var rows = await BuildMainRowAsync(propertyId, userId, ct);

            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged       = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore     = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            return (paged, hasMore);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section 1 — Main: all RentedNotice fields in one row.
        //   PTIS.PropertyMast PM
        //   PTIS.WardMaster   WM      (WardId = WM.Id)
        //   PTIS.SocietyDetailsMast SD (PM.Id = SD.PropertyId)
        //   PTIS.TypeOfUseMaster TUM  (PM.PropertyTypeId = TUM.Id)
        //   PTIS.PropertyMastOld PMO  (PM.PropertyMastOldId = PMO.Id)
        //   PTIS.PropertyTypeMaster PTM (PM.PropertyTypeId = PTM.Id)
        //   CORE.UlbMaster ULB  (first row)
        //   CORE.UserMaster USR (Id = @userId)
        //   + pivoted TransMast tax columns
        // ─────────────────────────────────────────────────────────────────────────
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

            // 1b. Ward: separate query to avoid nullable-int JOIN crash
            string? wardNo = null;
            if (property.WardId > 0)
            {
                wardNo = await _wardRepository.GetQueryable()
                    .Where(w => w.Id == property.WardId)
                    .Select(w => w.WardNo)
                    .FirstOrDefaultAsync(ct);
            }

            // 1c. Society details
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

            // 1d. Type-of-use
            var typeOfUse = property.PropertyTypeId.HasValue
                ? await _typeOfUseRepository.GetQueryable()
                    .Where(t => t.Id == property.PropertyTypeId.Value)
                    .Select(t => new { t.Description, t.TypeOfUseCode })
                    .FirstOrDefaultAsync(ct)
                : null;

            // 1e. PropertyMastOld — OldWardNo, OldPropertyNo, OldPartitionNo
            var propertyOld = property.PropertyMastOldId.HasValue
                ? await _propertyMastOldRepository.GetQueryable()
                    .Where(o => o.Id == property.PropertyMastOldId.Value)
                    .Select(o => new { o.OldWardNo, o.OldPropertyNo, o.OldPartitionNo })
                    .FirstOrDefaultAsync(ct)
                : null;

            // 1f. PropertyTypeMaster — PropertyDescription, Type, PartType
            var propertyType = property.PropertyTypeId.HasValue
                ? await _propertyTypeRepository.GetQueryable()
                    .Where(pt => pt.Id == property.PropertyTypeId.Value)
                    .Select(pt => new { pt.PropertyDescription, pt.Type, pt.PartType })
                    .FirstOrDefaultAsync(ct)
                : null;

            // 1g. ULB Master
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

            // 1h. User
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

            // 1i. TransMast pivot
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

            // 1j. PropertyDetails — first floor entry fields embedded in main row
            //     SELECT TOP 1 FloorId, SubFloorId, ConstructionYear, AssessmentYear,
            //                  CarpetAreaSqFeet, CarpetAreaSqMeter, BuiltupAreaSqFeet,
            //                  BuiltupAreaSqMeter, NoOfRooms
            //     FROM PTIS.PropertyDetails WHERE PropertyId = @propertyId
            var propertyDetail = await _propertyDetailsRepository.GetQueryable()
                .Where(pd => pd.PropertyId == propertyId)
                .Select(pd => new
                {
                    pd.FloorId,
                    pd.SubFloorId,
                    pd.ConstructionYear,
                    pd.AssessmentYear,
                    pd.CarpetAreaSqFeet,
                    pd.CarpetAreaSqMeter,
                    pd.BuiltupAreaSqFeet,
                    pd.BuiltupAreaSqMeter,
                    pd.NoOfRooms,
                })
                .FirstOrDefaultAsync(ct);

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
                // Type-of-use
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
                // User Master (CORE.UserMaster)
                ["userId"]                   = user?.Id,
                ["userName"]                 = user?.UserName,
                ["firstName"]                = user?.FirstName,
                ["middleName"]               = user?.MiddleName,
                ["lastName"]                 = user?.LastName,
                ["userCode"]                 = user?.UserCode,
                ["userEmail"]                = user?.Email,
                ["userMobileNo"]             = user?.MobileNo,
                // ULB Master (CORE.UlbMaster)
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
                // Property Details (PTIS.PropertyDetails — first floor entry)
                ["floorId"]                  = propertyDetail?.FloorId,
                ["subFloorId"]               = propertyDetail?.SubFloorId,
                ["constructionYear"]         = propertyDetail?.ConstructionYear,
                ["assessmentYear"]           = propertyDetail?.AssessmentYear,
                ["carpetAreaSqFeet"]         = propertyDetail?.CarpetAreaSqFeet,
                ["carpetAreaSqMeter"]        = propertyDetail?.CarpetAreaSqMeter,
                ["builtupAreaSqFeet"]        = propertyDetail?.BuiltupAreaSqFeet,
                ["builtupAreaSqMeter"]       = propertyDetail?.BuiltupAreaSqMeter,
                ["noOfRooms"]               = propertyDetail?.NoOfRooms,
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
        // ─────────────────────────────────────────────────────────────────────────
        // Section 2 — Property Details: one row per PTIS.PropertyDetails floor entry
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<List<object>> BuildPropertyDetailsRowsAsync(int propertyId, CancellationToken ct)
        {
            var details = await _propertyDetailsRepository.GetQueryable()
                .Where(pd => pd.PropertyId == propertyId)
                .Select(pd => new
                {
                    pd.PropertyId,
                    pd.FloorId,
                    pd.SubFloorId,
                    pd.ConstructionYear,
                    pd.AssessmentYear,
                    pd.CarpetAreaSqFeet,
                    pd.CarpetAreaSqMeter,
                    pd.BuiltupAreaSqFeet,
                    pd.BuiltupAreaSqMeter,
                    pd.NoOfRooms,
                })
                .ToListAsync(ct);

            return details.Select(pd => (object)new Dictionary<string, object?>
            {
                ["propertyId"]         = pd.PropertyId,
                ["floorId"]            = pd.FloorId,
                ["subFloorId"]         = pd.SubFloorId,
                ["constructionYear"]   = pd.ConstructionYear,
                ["assessmentYear"]     = pd.AssessmentYear,
                ["carpetAreaSqFeet"]   = pd.CarpetAreaSqFeet,
                ["carpetAreaSqMeter"]  = pd.CarpetAreaSqMeter,
                ["builtupAreaSqFeet"]  = pd.BuiltupAreaSqFeet,
                ["builtupAreaSqMeter"] = pd.BuiltupAreaSqMeter,
                ["noOfRooms"]          = pd.NoOfRooms,
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section 3 — Tax Details: one pivoted row of TransMast amounts
        //   SELECT TaxCode, TaxName, RVorCV, RVorCVValue, TaxAmount
        //   FROM PTIS.TransMast TM JOIN PTIS.TaxMaster TAM ON TM.TaxId = TAM.Id
        //   WHERE TM.PropertyId = @propertyId
        //   ORDER BY TAM.DisplayOrder
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<List<object>> BuildTaxDetailsRowsAsync(int propertyId, CancellationToken ct)
        {
            var taxRows = await (
                from tm  in _transmastRepository.GetQueryable().Where(t => t.PropertyId == propertyId)
                join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
                orderby tam.DisplayOrder
                select new
                {
                    tam.TaxCode,
                    tam.TaxName,
                    tam.DisplayOrder,
                    tm.RVorCV,
                    tm.RVorCVValue,
                    tm.TaxAmount,
                }
            ).ToListAsync(ct);

            if (!taxRows.Any())
                return new List<object>();

            var row = new Dictionary<string, object?>();
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
