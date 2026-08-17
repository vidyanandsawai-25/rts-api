using System;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// Karakarni report data provider.
    ///
    /// Produces four logical sections per certificate call:
    ///   "main"           — one row per property: property master + ward + society + type-of-use fields.
    ///   "propertyDetails"— one row per floor/sub-floor entry in PTIS.PropertyDetails.
    ///   "taxDetails"     — one row per tax line in PTIS.TransMast (pivoted with TaxName).
    ///   "floorDetails"   — one row per floor with FloorMaster + ConstructionTypeMaster + TypeOfUseMaster.
    ///
    /// Parameters: propertyId (int or comma-separated list), userId (int)
    /// Section discovery is static (no query runs during authenticate).
    /// </summary>
    public class KarakarniDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";
        public const string PropertyDetailsSection = "propertyDetails";
        public const string TaxDetailsSection = "taxDetails";
        public const string FloorDetailsSection = "floorDetails";

        public string ProviderCode => "KarakarniDataProvider";

        private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
        private readonly IReportDataRepository<WardEntity> _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity> _typeOfUseRepository;
        private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
        private readonly IReportDataRepository<FloorEntity> _floorRepository;
        private readonly IReportDataRepository<ConstructionTypeEntity> _constructionTypeRepository;
        private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity> _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity> _userRepository;
        private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
        // private readonly IReportDataRepository<TransMastEntity> _transRepository; // Unnecessary duplicate of _transmastRepository
        private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;

        public KarakarniDataProvider(
            IReportDataRepository<PropertyEntity> propertyRepository,
            IReportDataRepository<WardEntity> wardRepository,
            IReportDataRepository<SocietyDetailsEntity> societyRepository,
            IReportDataRepository<TypeOfUseEntity> typeOfUseRepository,
            IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
            IReportDataRepository<FloorEntity> floorRepository,
            IReportDataRepository<ConstructionTypeEntity> constructionTypeRepository,
            IReportDataRepository<TransMastEntity> transmastRepository,
            IReportDataRepository<TaxMasterEntity> taxMastRepository,
            IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
            IReportDataRepository<UserEntity> userRepository,
            IReportDataRepository<YearMasterEntity> yearRepository,
            // IReportDataRepository<TransMastEntity> transRepository, // Unnecessary duplicate of transmastRepository
            IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository
            )
        {
            _propertyRepository = propertyRepository;
            _wardRepository = wardRepository;
            _societyRepository = societyRepository;
            _typeOfUseRepository = typeOfUseRepository;
            _propertyDetailsRepository = propertyDetailsRepository;
            _floorRepository = floorRepository;
            _constructionTypeRepository = constructionTypeRepository;
            _transmastRepository = transmastRepository;
            _taxMastRepository = taxMastRepository;
            _ulbMasterRepository = ulbMasterRepository;
            _userRepository = userRepository;
            _yearRepository = yearRepository;
            // _transRepository = transRepository; // Unnecessary duplicate of _transmastRepository
            _ReportRequestRepository = reportRequestRepository;
            _propertyTypeRepository = propertyTypeRepository;
        }

        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection,            true),
            new ReportSectionDescriptor(PropertyDetailsSection, true),
            new ReportSectionDescriptor(TaxDetailsSection,      true),
            new ReportSectionDescriptor(FloorDetailsSection,    true),
        };

        public async Task<object> GetDataAsync(
            Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var financeYear = ParseFinanceYear(parameters);
            var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, MainSection, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(
            Guid reportRequestId,
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            var financeYear = ParseFinanceYear(parameters);
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 100;

            var (rows, hasMore) = await BuildPageAsync(reportRequestId, parameters, section, (page - 1) * pageSize, pageSize, ct);
            return new ReportDataPage
            {
                Section = section,
                Page = page,
                PageSize = pageSize,
                TotalCount = -1,
                HasMore = hasMore,
                Rows = rows,
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

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(Guid reportRequestId, Dictionary<string, string> parameters, string section, int skip, int take, CancellationToken ct)
        {
            parameters.TryGetValue("ownerId", out var ownerIdText);

            var ownerIds = string.IsNullOrWhiteSpace(ownerIdText) ? new List<int>() : ownerIdText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

            // --- Parse parameters ---
            parameters.TryGetValue("zoneId", out var zoneIdText);
            int.TryParse(zoneIdText, out var zoneId);

            parameters.TryGetValue("wardId", out var wardIdText);
            int.TryParse(wardIdText, out var wardId);

            parameters.TryGetValue("propertyNo", out var propertyNoText);
            propertyNoText = string.IsNullOrWhiteSpace(propertyNoText) ? null : propertyNoText.Trim();

            parameters.TryGetValue("partitionNo", out var partitionNoText);
            partitionNoText = string.IsNullOrWhiteSpace(partitionNoText) ? null : partitionNoText.Trim();

            parameters.TryGetValue("assessmentStatus", out var assessmentStatusText);
            int.TryParse(assessmentStatusText, out var assessmentStatus);

            // ------ FROM Property - TO Property Number Range Filter Parameters ------
            parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
            fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText)
                ? null
                : fromPropertyNoText.Trim();

            parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
            toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText)
                ? null
                : toPropertyNoText.Trim();

            // propertyId accepts a single value OR comma-separated list: "101,202,303" propertyid means owenerid
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId", out var userIdStr);

            parameters.TryGetValue("Type", out var type);
            type = string.IsNullOrWhiteSpace(type)
                ? null
                : type.Trim().ToUpper();

            parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
            int.TryParse(propertyTypeIdText, out var propertyTypeId);

            parameters.TryGetValue("PropertyDescription", out var propertyDescription);
            propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();

            var financeYear = ParseFinanceYear(parameters);
            var activeYear = await BaseQuery(financeYear).FirstOrDefaultAsync(ct);
            int activeYearId = activeYear?.Id ?? 0;

            // Split on commas, parse each token, deduplicate, drop invalid entries.
            var propertyIds = (propertyIdStr ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ownerIds.Count > 0)
            {
                propertyIds.AddRange(ownerIds);
                propertyIds = propertyIds.Distinct().ToList();
            }

            // If no explicit propertyIds are provided, resolve them via filters
            if (propertyIds.Count == 0)
            {
                var query =
                    from p in _propertyRepository.GetQueryable()
                    join w in _wardRepository.GetQueryable() on p.WardId equals w.Id into wj
                    from w in wj.DefaultIfEmpty()

                        // ---------------- JOIN PropertyTypeMaster ----------------
                    join pt in _propertyTypeRepository.GetQueryable()
                        on p.PropertyTypeId equals pt.Id into ptj
                    from pt in ptj.DefaultIfEmpty()

                    where p.IsActive && !p.MarkedForDeletion
                          && (zoneId == 0 || w.ZoneId == zoneId)
                          && (wardId == 0 || p.WardId == wardId)
                          && (propertyNoText == null || p.PropertyNo == propertyNoText)
                          && (partitionNoText == null || p.PartitionNo == partitionNoText)
                          && (assessmentStatus == 0 || p.PropertyAssessmentStatusId == assessmentStatus)
                          && (string.IsNullOrEmpty(type) || p.Type == type)
                          && (propertyTypeId == 0 || p.PropertyTypeId == propertyTypeId)
                          && (string.IsNullOrEmpty(propertyDescription) || pt.PropertyDescription == propertyDescription)
                          && (fromPropertyNoText == null || string.Compare(p.PropertyNo, fromPropertyNoText) >= 0)
                          && (toPropertyNoText == null || string.Compare(p.PropertyNo, toPropertyNoText) <= 0)

                    select p;

                // APPLY TransMast constraint SERVER-SIDE only when caller requested financeYear
                if (financeYear != 0 && activeYearId > 0)
                {
                    var transQ = _transmastRepository.GetQueryable()
                        .Where(t => t.FinanceYearId == activeYearId)
                        .Select(t => t.PropertyId);

                    query = query.Where(p => transQ.Contains(p.Id));
                }

                propertyIds = await query.Select(p => p.Id).ToListAsync(ct);
            }

            int.TryParse(userIdStr, out var userId);

            List<object> rows;

            switch (section)
            {
                case PropertyDetailsSection:
                    rows = await BuildPropertyDetailsRowsAsync(propertyIds, ct);
                    break;

                case TaxDetailsSection:
                    rows = await BuildTaxDetailsRowsAsync(propertyIds, activeYearId, ct);
                    break;

                case FloorDetailsSection:
                    rows = await BuildFloorDetailsRowsAsync(propertyIds, ct);
                    break;

                default: // MainSection
                    rows = await BuildMainRowsAsync(propertyIds, reportRequestId, activeYearId, activeYear?.YearCode ?? "", ct);
                    break;
            }

            // Apply skip/take (main always returns 1 row; details may have many).
            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            return (paged, hasMore);
        }

        private async Task<List<object>> BuildMainRowsAsync(List<int> propertyIds, Guid reportRequestId, int activeYearId, string yearCode, CancellationToken ct)
        {
            if (propertyIds == null || propertyIds.Count == 0)
                return new List<object>();

            // 1a. All properties + Ward (LEFT JOIN) in one batch
            var properties = await (
                from pm in _propertyRepository.GetQueryable()
                                               .Where(p => propertyIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
                join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wj
                from wm in wj.DefaultIfEmpty()
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
                    pm.PlotNo,
                    pm.FlatOrShopNo,
                    pm.FlatOrShopName,
                    pm.PropertyTypeId,
                    WardNo = wm != null ? wm.WardNo : null,
                }
            ).ToListAsync(ct);

            if (!properties.Any())
                return new List<object>();

            // 1b. Society details (batch load for all property IDs)
            var societyDetails = await _societyRepository.GetQueryable()
                .Where(sd => sd.PropertyId.HasValue && propertyIds.Contains(sd.PropertyId.Value))
                .Select(sd => new
                {
                    PropertyId = sd.PropertyId!.Value,
                    sd.WingId,
                    sd.WingName,
                    sd.SocietyName,
                    sd.SocietyAddress,
                })
                .ToListAsync(ct);

            var societyMap = societyDetails
                .GroupBy(sd => sd.PropertyId)
                .ToDictionary(g => g.Key, g => g.First());

            // 1c. Type-of-use (batch load for unique PropertyTypeIds)
            var uniqueTypeOfUseIds = properties
                .Where(p => p.PropertyTypeId.HasValue)
                .Select(p => p.PropertyTypeId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, dynamic> typeOfUseMap;
            if (uniqueTypeOfUseIds.Any())
            {
                var typeOfUseData = await _typeOfUseRepository.GetQueryable()
                    .Where(t => uniqueTypeOfUseIds.Contains(t.Id))
                    .Select(t => new
                    {
                        t.Id,
                        t.Description,
                        t.TypeOfUseCode,
                    })
                    .ToListAsync(ct);
                typeOfUseMap = typeOfUseData.ToDictionary(t => t.Id, t => (dynamic)t);
            }
            else
            {
                typeOfUseMap = new Dictionary<int, dynamic>();
            }

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

            var taxRowsAll = await (
                from tm in _transmastRepository.GetQueryable()
                    .Where(t => propertyIds.Contains(t.PropertyId) && (activeYearId == 0 || t.FinanceYearId == activeYearId))
                join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
                orderby tam.DisplayOrder
                select new
                {
                    tm.PropertyId,
                    tam.TaxCode,
                    tam.TaxName,
                    tm.RVorCV,
                    tm.RVorCVValue,
                    tm.TaxAmount,
                }
            ).ToListAsync(ct);

            var taxRowsByProperty = taxRowsAll.GroupBy(t => t.PropertyId)
                                              .ToDictionary(g => g.Key, g => g.ToList());

            // Build one row per property
            var allRows = new List<object>();

            foreach (var property in properties)
            {
                var society = societyMap.ContainsKey(property.Id) ? societyMap[property.Id] : null;
                var typeOfUse = property.PropertyTypeId.HasValue && typeOfUseMap.ContainsKey(property.PropertyTypeId.Value)
                    ? typeOfUseMap[property.PropertyTypeId.Value]
                    : null;

                List<dynamic> taxRows;
                if (taxRowsByProperty.ContainsKey(property.Id))
                {
                    taxRows = taxRowsByProperty[property.Id].Cast<dynamic>().ToList();
                }
                else
                {
                    taxRows = new List<dynamic>();
                }

                var row = new Dictionary<string, object?>
                {
                    ["propertyId"] = property.Id,
                    ["propertyNo"] = property.PropertyNo,
                    ["wardId"] = property.WardId,
                    ["wardNo"] = property.WardNo,
                    ["partitionNo"] = property.PartitionNo,
                    ["upicId"] = property.UPICId,
                    ["subZoneNo"] = property.SubZoneNo,
                    ["mobileNo"] = property.MobileNo,
                    ["ownerTitle"] = property.OwnerTitle,
                    ["occupierTitle"] = property.OccupierTitle,
                    ["ownerName"] = property.OwnerName,
                    ["occupierName"] = property.OccupierName,
                    ["address"] = property.Address,
                    ["plotNo"] = property.PlotNo,
                    ["flatOrShopNo"] = property.FlatOrShopNo,
                    ["flatOrShopName"] = property.FlatOrShopName,
                    // Society details
                    ["wingId"] = society?.WingId,
                    ["wingName"] = society?.WingName,
                    ["societyName"] = society?.SocietyName,
                    ["societyAddress"] = society?.SocietyAddress,
                    // Type-of-use
                    ["typeOfUseDesc"] = typeOfUse?.Description,
                    ["typeOfUseCode"] = typeOfUse?.TypeOfUseCode,
                    // User Master fields (CORE.UserMaster)
                    ["userId"] = user?.Id,
                    ["userName"] = user?.UserName,
                    ["userCode"] = user?.UserCode,
                    ["userEmail"] = user?.Email,
                    ["userMobileNo"] = user?.MobileNo,
                    // ULB Master fields (CORE.UlbMaster)
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
                    ["financeYear"] = yearCode,
                };

                // Pivot TransMast rows into dynamic columns on the same main row.
                // Column naming: Transmast_{SafeName} / RVorCV_{SafeName} / RVorCVValue_{SafeName}
                // Uses TaxCode (spaces → '_') as the safe key; falls back to TaxName.
                // Example: "Big Building" → Transmast_Big_Building, RVorCV_Big_Building, RVorCVValue_Big_Building
                foreach (var tax in taxRows)
                {
                    var safeCode = (tax.TaxCode?.Trim().Length > 0
                                        ? tax.TaxCode
                                        : tax.TaxName ?? "UNKNOWN")
                                   .Replace(' ', '_');

                    row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                    row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                    row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
                }

                allRows.Add(row);
            }

            return allRows;
        }

        private async Task<List<object>> BuildPropertyDetailsRowsAsync(List<int> propertyIds, CancellationToken ct)
        {
            if (propertyIds == null || propertyIds.Count == 0)
                return new List<object>();

            var details = await (
                from pd in _propertyDetailsRepository.GetQueryable()
                                                     .Where(p => propertyIds.Contains(p.PropertyId))
                join fm in _floorRepository.GetQueryable()
                         on pd.FloorId equals fm.Id into fmj
                from fm in fmj.DefaultIfEmpty()
                join ctm in _constructionTypeRepository.GetQueryable()
                         on pd.ConstructionTypeId equals ctm.Id into ctmj
                from ctm in ctmj.DefaultIfEmpty()
                join tum in _typeOfUseRepository.GetQueryable()
                         on pd.TypeOfUseId equals tum.Id into tumj
                from tum in tumj.DefaultIfEmpty()
                select new
                {
                    pd.PropertyId,
                    pd.FloorId,
                    pd.SubFloorId,
                    FloorDescription = fm != null ? fm.Description : null,
                    pd.ConstructionYear,
                    ConstructionCode = ctm != null ? ctm.ConstructionCode : null,
                    ConstructionDescription = ctm != null ? ctm.Description : null,
                    TypeOfUseCode = tum != null ? tum.TypeOfUseCode : null,
                    TypeOfUseDescription = tum != null ? tum.Description : null,
                    TypeOfUseType = tum != null ? tum.Type : null,
                    pd.AssessmentYear,
                    pd.CarpetAreaSqFeet,
                    pd.CarpetAreaSqMeter,
                    pd.BuiltupAreaSqFeet,
                    pd.BuiltupAreaSqMeter,
                    pd.NoOfRooms,
                }
            ).ToListAsync(ct);

            return details.Select(pd => (object)new Dictionary<string, object?>
            {
                ["propertyId"] = pd.PropertyId,
                ["floorId"] = pd.FloorId,
                ["subFloorId"] = pd.SubFloorId,
                ["floorDescription"] = pd.FloorDescription,
                ["constructionYear"] = pd.ConstructionYear,
                ["constructionCode"] = pd.ConstructionCode,
                ["constructionDescription"] = pd.ConstructionDescription,
                ["typeOfUseCode"] = pd.TypeOfUseCode,
                ["typeOfUseDescription"] = pd.TypeOfUseDescription,
                ["typeOfUseType"] = pd.TypeOfUseType,
                ["assessmentYear"] = pd.AssessmentYear,
                ["carpetAreaSqFeet"] = pd.CarpetAreaSqFeet,
                ["carpetAreaSqMeter"] = pd.CarpetAreaSqMeter,
                ["builtupAreaSqFeet"] = pd.BuiltupAreaSqFeet,
                ["builtupAreaSqMeter"] = pd.BuiltupAreaSqMeter,
                ["noOfRooms"] = pd.NoOfRooms,
            }).ToList();
        }

        private async Task<List<object>> BuildTaxDetailsRowsAsync(List<int> propertyIds, int activeYearId, CancellationToken ct)
        {
            if (propertyIds == null || propertyIds.Count == 0)
                return new List<object>();

            var taxRows = await (
                from tm in _transmastRepository.GetQueryable()
                    .Where(t => propertyIds.Contains(t.PropertyId) && (activeYearId == 0 || t.FinanceYearId == activeYearId))
                join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
                orderby tam.DisplayOrder
                select new
                {
                    tm.PropertyId,
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

            // Group by PropertyId and pivot each property's tax lines into a separate row
            var result = new List<object>();
            var taxRowsByProperty = taxRows.GroupBy(t => t.PropertyId);

            foreach (var propertyTaxGroup in taxRowsByProperty)
            {
                // Pivot all tax lines into a single row with dynamic column names.
                // Uses TaxCode (spaces → '_') as the safe key; falls back to TaxName if TaxCode is blank.
                var row = new Dictionary<string, object?>
                {
                    ["propertyId"] = propertyTaxGroup.Key
                };

                foreach (var tax in propertyTaxGroup)
                {
                    var safeCode = (tax.TaxCode?.Trim().Length > 0
                                        ? tax.TaxCode
                                        : tax.TaxName ?? "UNKNOWN")
                                   .Replace(' ', '_');

                    row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                    row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                    row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
                }

                result.Add(row);
            }

            return result;
        }

        private async Task<List<object>> BuildFloorDetailsRowsAsync(List<int> propertyIds, CancellationToken ct)
        {
            if (propertyIds == null || propertyIds.Count == 0)
                return new List<object>();

            var rows = await (
                from pd in _propertyDetailsRepository.GetQueryable()
                                                      .Where(p => propertyIds.Contains(p.PropertyId))
                join fm in _floorRepository.GetQueryable()
                         on pd.FloorId equals fm.Id into fmj
                from fm in fmj.DefaultIfEmpty()
                join ctm in _constructionTypeRepository.GetQueryable()
                         on pd.ConstructionTypeId equals ctm.Id into ctmj
                from ctm in ctmj.DefaultIfEmpty()
                join tum in _typeOfUseRepository.GetQueryable()
                         on pd.TypeOfUseId equals tum.Id into tumj
                from tum in tumj.DefaultIfEmpty()
                select new
                {
                    pd.PropertyId,
                    FloorDescription = fm != null ? fm.Description : null,
                    pd.ConstructionYear,
                    ConstructionCode = ctm != null ? ctm.ConstructionCode : null,
                    ConstructionDescription = ctm != null ? ctm.Description : null,
                    TypeOfUseCode = tum != null ? tum.TypeOfUseCode : null,
                    TypeOfUseDescription = tum != null ? tum.Description : null,
                    TypeOfUseType = tum != null ? tum.Type : null,
                    pd.CarpetAreaSqMeter,
                    pd.CarpetAreaSqFeet,
                    pd.BuiltupAreaSqMeter,
                    pd.BuiltupAreaSqFeet,
                    pd.NoOfRooms,
                }
            ).ToListAsync(ct);

            return rows.Select(r => (object)new Dictionary<string, object?>
            {
                ["propertyId"] = r.PropertyId,
                ["floorDescription"] = r.FloorDescription,
                ["constructionYear"] = r.ConstructionYear,
                ["constructionCode"] = r.ConstructionCode,
                ["constructionDescription"] = r.ConstructionDescription,
                ["typeOfUseCode"] = r.TypeOfUseCode,
                ["typeOfUseDescription"] = r.TypeOfUseDescription,
                ["typeOfUseType"] = r.TypeOfUseType,
                ["carpetAreaSqMeter"] = r.CarpetAreaSqMeter,
                ["carpetAreaSqFeet"] = r.CarpetAreaSqFeet,
                ["builtupAreaSqMeter"] = r.BuiltupAreaSqMeter,
                ["builtupAreaSqFeet"] = r.BuiltupAreaSqFeet,
                ["noOfRooms"] = r.NoOfRooms,
            }).ToList();
        }
    }
}
