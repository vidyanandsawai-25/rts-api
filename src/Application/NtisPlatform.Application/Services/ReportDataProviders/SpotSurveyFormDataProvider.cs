using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// SpotSurveyForm report data provider.
    ///
    /// Produces four logical sections per certificate call:
    ///   "main"           — one row: property master + ward + society + type-of-use fields.
    ///   "propertyDetails"— one row per floor/sub-floor entry in PTIS.PropertyDetails.
    ///   "taxDetails"     — one row per tax line in PTIS.TransMast (pivoted with TaxName).
    ///   "floorDetails"   — one row per floor with FloorMaster + ConstructionTypeMaster + TypeOfUseMaster.
    ///
    /// Section discovery is static (no query runs during authenticate).
    /// </summary>
    public class SpotSurveyFormDataProvider : IPagedReportDataProvider
    {
        public const string MainSection            = "main";
        //public const string PropertyDetailsSection = "propertyDetails";
        //public const string TaxDetailsSection      = "taxDetails";
        public const string FloorDetailsSection    = "floorDetails";

        public string ProviderCode => "SpotSurveyFormDataProvider";

        private readonly IReportDataRepository<PropertyEntity>        _propertyRepository;
        private readonly IReportDataRepository<WardEntity>            _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity>  _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity>       _typeOfUseRepository;
        private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
        private readonly IReportDataRepository<FloorEntity>           _floorRepository;
        private readonly IReportDataRepository<ConstructionTypeEntity> _constructionTypeRepository;
        private readonly IReportDataRepository<TransMastEntity>       _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity>       _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity>       _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity>            _userRepository;

        public SpotSurveyFormDataProvider(
            IReportDataRepository<PropertyEntity>        propertyRepository,
            IReportDataRepository<WardEntity>            wardRepository,
            IReportDataRepository<SocietyDetailsEntity>  societyRepository,
            IReportDataRepository<TypeOfUseEntity>       typeOfUseRepository,
            IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
            IReportDataRepository<FloorEntity>           floorRepository,
            IReportDataRepository<ConstructionTypeEntity> constructionTypeRepository,
            IReportDataRepository<TransMastEntity>       transmastRepository,
            IReportDataRepository<TaxMasterEntity>       taxMastRepository,
            IReportDataRepository<ULBMasterEntity>       ulbMasterRepository,
            IReportDataRepository<UserEntity>            userRepository)
        {
            _propertyRepository         = propertyRepository;
            _wardRepository             = wardRepository;
            _societyRepository          = societyRepository;
            _typeOfUseRepository        = typeOfUseRepository;
            _propertyDetailsRepository  = propertyDetailsRepository;
            _floorRepository            = floorRepository;
            _constructionTypeRepository = constructionTypeRepository;
            _transmastRepository        = transmastRepository;
            _taxMastRepository          = taxMastRepository;
            _ulbMasterRepository        = ulbMasterRepository;
            _userRepository             = userRepository;
        }

        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection,            false),
            //new ReportSectionDescriptor(PropertyDetailsSection, false),
            //new ReportSectionDescriptor(TaxDetailsSection,      false),
            new ReportSectionDescriptor(FloorDetailsSection,    false),
        };

        public async Task<object> GetDataAsync(
            Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var (rows, _) = await BuildPageAsync(parameters, MainSection, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            if (page     < 1)  page     = 1;
            if (pageSize <= 0) pageSize = 100;

            var (rows, hasMore) = await BuildPageAsync(parameters, section, (page - 1) * pageSize, pageSize, ct);
            return new ReportDataPage
            {
                Section    = section,
                Page       = page,
                PageSize   = pageSize,
                TotalCount = -1,
                HasMore    = hasMore,
                Rows       = rows,
            };
        }

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Dictionary<string, string> parameters, string section, int skip, int take, CancellationToken ct)
        {
            // --- Parse parameters ---
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId",     out var userIdStr);
            int.TryParse(propertyIdStr, out var propertyId);
            int.TryParse(userIdStr,     out var userId);

            List<object> rows;

            switch (section)
            {
                //case PropertyDetailsSection:
                //    rows = await BuildPropertyDetailsRowsAsync(propertyId, ct);
                //    break;

                //case TaxDetailsSection:
                //    rows = await BuildTaxDetailsRowsAsync(propertyId, ct);
                //    break;

                case FloorDetailsSection:
                    rows = await BuildFloorDetailsRowsAsync(propertyId, ct);
                    break;

                default: // MainSection
                    rows = await BuildMainRowAsync(propertyId, userId, ct);
                    break;
            }

            // Apply skip/take (main always returns 1 row; details may have many).
            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged       = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore     = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            return (paged, hasMore);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section 1 — Main: property master + ward + society + type-of-use + ULB
        // Equivalent SQL:
        //   SELECT PM.*, WM.WardNo, SD.WingId/WingName/SocietyName/SocietyAddress,
        //          TUM.Description, TUM.TypeOfUseCode,
        //          ULB.UlbCode, ULB.UlbName, ... (CORE.UlbMaster)
        //          + pivoted: Transmast_{TaxName} = TaxAmount, RVorCV_{TaxName}, RVorCVValue_{TaxName}
        //   FROM   PTIS.PropertyMast PM
        //   LEFT JOIN PTIS.WardMaster WM          ON PM.WardId        = WM.Id
        //   LEFT JOIN PTIS.SocietyDetailsMast SD  ON PM.Id            = SD.PropertyId
        //   LEFT JOIN PTIS.TypeOfUseMaster TUM    ON PM.PropertyTypeId = TUM.Id
        //   CROSS JOIN CORE.UlbMaster ULB (first row)
        //   + TransMast JOIN TaxMaster (pivoted in memory)
        //   WHERE PM.Id = @propertyId
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<List<object>> BuildMainRowAsync(int propertyId, int userId, CancellationToken ct)
        {
            // 1a. Property + Ward (LEFT JOIN)
            var property = await (
                from pm in _propertyRepository.GetQueryable().Where(p => p.Id == propertyId)
                join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wmj
                from wm in wmj.DefaultIfEmpty()
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
                    pm.FlatOrShopNo,
                    pm.FlatOrShopName,
                    pm.PropertyTypeId,
                    WardNo = wm != null ? wm.WardNo : null,
                }
            ).FirstOrDefaultAsync(ct);

            if (property == null)
                return new List<object>();

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

            // 1f. TransMast pivot — fetch all tax lines for this property, then
            //     write dynamic columns: Transmast_{TaxName} / RVorCV_{TaxName} / RVorCVValue_{TaxName}
            //     SQL: SELECT TAM.TaxCode, TAM.TaxName, TM.RVorCV, TM.RVorCVValue, TM.TaxAmount
            //          FROM PTIS.TransMast TM JOIN PTIS.TaxMaster TAM ON TM.TaxId = TAM.Id
            //          WHERE TM.PropertyId = @propertyId ORDER BY TAM.DisplayOrder
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
                ["propertyId"]           = property.Id,
                ["propertyNo"]           = property.PropertyNo,
                ["wardId"]               = property.WardId,
                ["wardNo"]               = property.WardNo,
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
                ["wingId"]               = society?.WingId,
                ["wingName"]             = society?.WingName,
                ["societyName"]          = society?.SocietyName,
                ["societyAddress"]       = society?.SocietyAddress,
                // Type-of-use
                ["typeOfUseDesc"]        = typeOfUse?.Description,
                ["typeOfUseCode"]        = typeOfUse?.TypeOfUseCode,
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

            // Pivot TransMast rows into dynamic columns on the same main row.
            // Column naming: Transmast_{SafeName} / RVorCV_{SafeName} / RVorCVValue_{SafeName}
            // Uses TaxCode (spaces → '_') as the safe key; falls back to TaxName.
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
        // Section 2 — Property Details: one row per floor entry
        // Equivalent SQL:
        //   SELECT PD.PropertyId, PD.FloorId, PD.SubFloorId,
        //          PD.ConstructionYear, PD.AssessmentYear,
        //          PD.CarpetAreaSqFeet, PD.CarpetAreaSqMeter,
        //          PD.BuiltupAreaSqFeet, PD.BuiltupAreaSqMeter, PD.NoOfRooms
        //   FROM PTIS.PropertyDetails PD
        //   WHERE PD.PropertyId = @propertyId
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
                ["propertyId"]        = pd.PropertyId,
                ["floorId"]           = pd.FloorId,
                ["subFloorId"]        = pd.SubFloorId,
                ["constructionYear"]  = pd.ConstructionYear,
                ["assessmentYear"]    = pd.AssessmentYear,
                ["carpetAreaSqFeet"]  = pd.CarpetAreaSqFeet,
                ["carpetAreaSqMeter"] = pd.CarpetAreaSqMeter,
                ["builtupAreaSqFeet"] = pd.BuiltupAreaSqFeet,
                ["builtupAreaSqMeter"]= pd.BuiltupAreaSqMeter,
                ["noOfRooms"]         = pd.NoOfRooms,
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section 3 — Tax Details: pivoted TransMast rows
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

            // Pivot all tax lines into a single row with dynamic column names.
            // Uses TaxCode (spaces → '_') as the safe key; falls back to TaxName if TaxCode is blank.
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

        // ─────────────────────────────────────────────────────────────────────────
        // Section 4 — Floor Details: one row per PropertyDetails entry with joined
        // FloorMaster, ConstructionTypeMaster and TypeOfUseMaster.
        //
        // Equivalent SQL:
        //   SELECT FM.Description, PD.ConstructionYear,
        //          CTM.ConstructionCode, CTM.Description,
        //          TUM.TypeOfUseCode, TUM.Description, TUM.Type,
        //          PD.CarpetAreaSqMeter, PD.CarpetAreaSqFeet,
        //          PD.BuiltupAreaSqMeter, PD.BuiltupAreaSqFeet, PD.NoOfRooms
        //   FROM PTIS.PropertyMast PM
        //   LEFT JOIN PTIS.PropertyDetails PD   ON PM.Id                 = PD.PropertyId
        //   LEFT JOIN PTIS.FloorMaster FM        ON PD.FloorId            = FM.Id
        //   LEFT JOIN PTIS.ConstructionTypeMaster CTM ON CTM.Id           = PD.ConstructionTypeId
        //   LEFT JOIN PTIS.TypeOfUseMaster TUM   ON PM.PropertyTypeId     = TUM.Id  (per PD.TypeOfUseId)
        //   WHERE PM.Id = @propertyId
        // ─────────────────────────────────────────────────────────────────────────
        private async Task<List<object>> BuildFloorDetailsRowsAsync(int propertyId, CancellationToken ct)
        {
            var rows = await (
                from pd  in _propertyDetailsRepository.GetQueryable()
                                                      .Where(p => p.PropertyId == propertyId)
                join fm  in _floorRepository.GetQueryable()
                         on pd.FloorId equals fm.Id into fmj
                from fm  in fmj.DefaultIfEmpty()
                join ctm in _constructionTypeRepository.GetQueryable()
                         on pd.ConstructionTypeId equals ctm.Id into ctmj
                from ctm in ctmj.DefaultIfEmpty()
                join tum in _typeOfUseRepository.GetQueryable()
                         on pd.TypeOfUseId equals tum.Id into tumj
                from tum in tumj.DefaultIfEmpty()
                select new
                {
                    FloorDescription        = fm  != null ? fm.Description  : null,
                    pd.ConstructionYear,
                    ConstructionCode        = ctm != null ? ctm.ConstructionCode : null,
                    ConstructionDescription = ctm != null ? ctm.Description  : null,
                    TypeOfUseCode           = tum != null ? tum.TypeOfUseCode : null,
                    TypeOfUseDescription    = tum != null ? tum.Description  : null,
                    TypeOfUseType           = tum != null ? tum.Type          : null,
                    pd.CarpetAreaSqMeter,
                    pd.CarpetAreaSqFeet,
                    pd.BuiltupAreaSqMeter,
                    pd.BuiltupAreaSqFeet,
                    pd.NoOfRooms,
                }
            ).ToListAsync(ct);

            return rows.Select(r => (object)new Dictionary<string, object?>
            {
                ["floorDescription"]        = r.FloorDescription,
                ["constructionYear"]         = r.ConstructionYear,
                ["constructionCode"]         = r.ConstructionCode,
                ["constructionDescription"]  = r.ConstructionDescription,
                ["typeOfUseCode"]            = r.TypeOfUseCode,
                ["typeOfUseDescription"]     = r.TypeOfUseDescription,
                ["typeOfUseType"]            = r.TypeOfUseType,
                ["carpetAreaSqMeter"]        = r.CarpetAreaSqMeter,
                ["carpetAreaSqFeet"]         = r.CarpetAreaSqFeet,
                ["builtupAreaSqMeter"]       = r.BuiltupAreaSqMeter,
                ["builtupAreaSqFeet"]        = r.BuiltupAreaSqFeet,
                ["noOfRooms"]                = r.NoOfRooms,
            }).ToList();
        }
    }
}
