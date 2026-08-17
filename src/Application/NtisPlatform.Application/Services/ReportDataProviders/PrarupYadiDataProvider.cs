using System;
using DocumentFormat.OpenXml.Spreadsheet;
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
    /// PrarupYadi report data provider.
    /// 
    /// Produces three logical sections per call:
    ///   "main"            — one row per property: all RentedNotice fields
    ///                       (property + ward + society + typeOfUse + propertyOld +
    ///                        propertyTypeMaster + ULB + user + pivoted TransMast columns)
    ///   "propertyDetails" — one row per PTIS.PropertyDetails floor entry
    ///   "taxDetails"      — one pivoted row of TransMast amounts keyed by TaxCode
    /// 
    /// Parameters: propertyId (int or comma-separated list), userId (int)
    /// Section discovery is static (no query runs during authenticate).
    /// </summary>
    public class PrarupYadiDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";
        public const string PropertyDetailsSection = "propertyDetails";
        public const string TaxDetailsSection = "taxDetails";
        public const string PrarupYadiFormDetailsSection = "PrarupYadiFormDetails";
        public const string ReportDataSection = "ReportData";

        public string ProviderCode => "PrarupYadiDataProvider";

        private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
        private readonly IReportDataRepository<WardEntity> _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity> _typeOfUseRepository;
        private readonly IReportDataRepository<PropertyMastOldEntity> _propertyMastOldRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;
        private readonly IReportDataRepository<PropertyDetailsEntity> _propertyDetailsRepository;
        private readonly IReportDataRepository<FloorEntity> _floorRepository;
        private readonly IReportDataRepository<ConstructionTypeEntity> _constructionTypeRepository;
        private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity> _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity> _userRepository;
        private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
        private readonly IReportDataRepository<TransMastEntity> _transRepository;
        private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
        private readonly IReportDataRepository<PropertyMapDetailEntity> _propertyMapDetailRepository;

        public PrarupYadiDataProvider(
            IReportDataRepository<PropertyEntity> propertyRepository,
            IReportDataRepository<WardEntity> wardRepository,
            IReportDataRepository<SocietyDetailsEntity> societyRepository,
            IReportDataRepository<TypeOfUseEntity> typeOfUseRepository,
            IReportDataRepository<PropertyMastOldEntity> propertyMastOldRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository,
            IReportDataRepository<PropertyDetailsEntity> propertyDetailsRepository,
            IReportDataRepository<FloorEntity> floorRepository,
            IReportDataRepository<ConstructionTypeEntity> constructionTypeRepository,
            IReportDataRepository<TransMastEntity> transmastRepository,
            IReportDataRepository<TaxMasterEntity> taxMastRepository,
            IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
            IReportDataRepository<UserEntity> userRepository,
            IReportDataRepository<YearMasterEntity> yearRepository,
            IReportDataRepository<TransMastEntity> transRepository,
            IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
            IReportDataRepository<PropertyMapDetailEntity> propertyMapDetailRepository)
        {
            _propertyRepository = propertyRepository;
            _wardRepository = wardRepository;
            _societyRepository = societyRepository;
            _typeOfUseRepository = typeOfUseRepository;
            _propertyMastOldRepository = propertyMastOldRepository;
            _propertyTypeRepository = propertyTypeRepository;
            _propertyDetailsRepository = propertyDetailsRepository;
            _floorRepository = floorRepository;
            _constructionTypeRepository = constructionTypeRepository;
            _transmastRepository = transmastRepository;
            _taxMastRepository = taxMastRepository;
            _ulbMasterRepository = ulbMasterRepository;
            _userRepository = userRepository;
            _yearRepository = yearRepository;
            _transRepository = transRepository;
            _ReportRequestRepository = reportRequestRepository;
            _propertyMapDetailRepository = propertyMapDetailRepository;
        }

        // Static — never runs a query (avoids any heavy query executing on the authenticate request).
        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection,            false),
            new ReportSectionDescriptor(PropertyDetailsSection, false),
            new ReportSectionDescriptor(PrarupYadiFormDetailsSection, false),
            new ReportSectionDescriptor(ReportDataSection,      false),
        };

        public async Task<object> GetDataAsync(
            Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var financeYear = ParseFinanceYear(parameters);
            var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(
            Guid reportRequestId,
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
            var financeYear = ParseFinanceYear(parameters);
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 100;

            // --- Parse parameters ---
            var propertyIds = await ResolvePropertyIdsAsync(parameters, ct);

            List<object> rows;
            switch (section)
            {
                case PropertyDetailsSection:
                case PrarupYadiFormDetailsSection:
                case ReportDataSection:
                    rows = await BuildPropertyDetailsRowsAsync(propertyIds, ct);
                    break;

                default: // MainSection (tax details are embedded in main)
                    rows = await BuildMainRowsAsync(propertyIds, reportRequestId, ct);
                    break;
            }

            var skip = (page - 1) * pageSize;
            var takePlusOne = pageSize + 1;
            var paged = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore = paged.Count > pageSize;
            if (hasMore) paged = paged.Take(pageSize).ToList();

            return new ReportDataPage
            {
                Section = section,
                Page = page,
                PageSize = pageSize,
                TotalCount = -1,
                HasMore = hasMore,
                Rows = paged,
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



        private async Task<List<int>> ResolvePropertyIdsAsync(Dictionary<string, string> parameters, CancellationToken ct)
        {
            parameters.TryGetValue("ownerId", out var ownerIdText);

            var ownerIds = string.IsNullOrWhiteSpace(ownerIdText) ? new List<int>() : ownerIdText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

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

          


            parameters.TryGetValue("fromPropertyNo", out var fromPropertyNoText);
            fromPropertyNoText = string.IsNullOrWhiteSpace(fromPropertyNoText)
                ? null
                : fromPropertyNoText.Trim();

            parameters.TryGetValue("toPropertyNo", out var toPropertyNoText);
            toPropertyNoText = string.IsNullOrWhiteSpace(toPropertyNoText)
                ? null
                : toPropertyNoText.Trim();

            parameters.TryGetValue("Type", out var type);
            type = string.IsNullOrWhiteSpace(type)
                ? null
                : type.Trim().ToUpper();

            parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
            int.TryParse(propertyTypeIdText, out var propertyTypeId);

            parameters.TryGetValue("PropertyDescription", out var propertyDescription);
            propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();


            var financeYear = ParseFinanceYear(parameters);
            int activeYearId = 0;
            if (financeYear != 0)
            {
                activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);
            }


            parameters.TryGetValue("propertyId", out var propertyIdStr);

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
                    var transQ = _transRepository.GetQueryable()
                        .Where(t => t.FinanceYearId == activeYearId)
                        .Select(t => t.PropertyId);

                    query = query.Where(p => transQ.Contains(p.Id));
                }

                propertyIds = await query.Select(p => p.Id).ToListAsync(ct);
            }

            return propertyIds;
        }

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Guid reportRequestId,
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            // --- Parse parameters ---
            var propertyIds = await ResolvePropertyIdsAsync(parameters, ct);

            var rows = await BuildMainRowsAsync(propertyIds, reportRequestId, ct);

            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var paged = rows.Skip(skip).Take(takePlusOne).ToList();
            var hasMore = take != int.MaxValue && paged.Count > take;
            if (hasMore) paged = paged.Take(take).ToList();

            return (paged, hasMore);
        }

        private async Task<List<object>> BuildMainRowsAsync(List<int> propertyIds, Guid reportRequestId, CancellationToken ct)
        {
            if (propertyIds == null || propertyIds.Count == 0)
                return new List<object>();

            // 1a. Properties — batch fetch all properties matching the IDs
            var properties = await _propertyRepository.GetQueryable()
                .Where(p => propertyIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
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
                })
                .ToListAsync(ct);

            if (!properties.Any())
                return new List<object>();

            // 1b. Ward mapping — collect unique ward IDs and resolve WardNo
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

            // 1c. Society details — batch fetch for all properties
            var allPropertyIds = properties.Select(p => p.Id).ToList();
            var societies = await _societyRepository.GetQueryable()
                .Where(sd => sd.PropertyId.HasValue && allPropertyIds.Contains(sd.PropertyId.Value))
                .Select(sd => new
                {
                    PropertyId = sd.PropertyId!.Value,
                    sd.WingId,
                    sd.WingName,
                    sd.SocietyName,
                    sd.SocietyAddress,
                })
                .ToListAsync(ct);
            var societyMap = societies.ToDictionary(s => s.PropertyId);

            // 1d. Type-of-use — batch fetch for all property types
            var uniquePropertyTypeIds = properties
                .Where(p => p.PropertyTypeId.HasValue)
                .Select(p => p.PropertyTypeId!.Value)
                .Distinct()
                .ToList();

            var typeOfUses = await _typeOfUseRepository.GetQueryable()
                .Where(t => uniquePropertyTypeIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Description, t.TypeOfUseCode })
                .ToListAsync(ct);
            var typeOfUseMap = typeOfUses.ToDictionary(t => t.Id);

            // 1e. Map new property IDs to old property IDs via PropertyMapDetail
            var propertyMappings = await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd => pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value) && pmd.IsActive && pmd.IsCurrent && pmd.Status == "ACTIVE")
                .Select(pmd => new { pmd.PropertyIdNew, pmd.PropertyIdOld })
                .ToListAsync(ct);

            var newToOldIdMap = propertyMappings
                .Where(m => m.PropertyIdNew.HasValue && m.PropertyIdOld.HasValue)
                .GroupBy(m => m.PropertyIdNew!.Value)
                .ToDictionary(g => g.Key, g => g.First().PropertyIdOld!.Value);

            var uniquePropertyMastOldIds = newToOldIdMap.Values.Distinct().ToList();

            var propertyOlds = await _propertyMastOldRepository.GetQueryable()
                .Where(o => uniquePropertyMastOldIds.Contains(o.Id))
                .Select(o => new { o.Id, o.OldWardNo, o.OldPropertyNo, o.OldPartitionNo })
                .ToListAsync(ct);
            var propertyOldMap = propertyOlds.ToDictionary(o => o.Id);

            // 1f. PropertyTypeMaster — batch fetch
            var propertyTypes = await _propertyTypeRepository.GetQueryable()
                .Where(pt => uniquePropertyTypeIds.Contains(pt.Id))
                .Select(pt => new { pt.Id, pt.PropertyDescription, pt.Type, pt.PartType })
                .ToListAsync(ct);
            var propertyTypeMap = propertyTypes.ToDictionary(pt => pt.Id);

            // 1g. ULB Master (shared across all rows)
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

            // 1h. User (shared across all rows)
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

            // 1i. TransMast pivot — batch fetch for all properties
            var allTaxRows = await (
                from tm in _transmastRepository.GetQueryable().Where(t => allPropertyIds.Contains(t.PropertyId))
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
            var taxRowsByProperty = allTaxRows.GroupBy(t => t.PropertyId).ToDictionary(g => g.Key, g => g.ToList());

            // 1j. PropertyDetails — batch fetch first floor entry for all properties
            var allPropertyDetailsRaw = await _propertyDetailsRepository.GetQueryable()
                .Where(pd => allPropertyIds.Contains(pd.PropertyId))
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

            // Group in memory to get first entry per property
            var propertyDetailsMap = allPropertyDetailsRaw
                .GroupBy(pd => pd.PropertyId)
                .ToDictionary(g => g.Key, g => g.First());

            // Build one row per property
            var rows = new List<object>();
            foreach (var property in properties)
            {
                var wardNo = property.WardId > 0 && wardMap.ContainsKey(property.WardId)
                    ? wardMap[property.WardId]
                    : null;

                societyMap.TryGetValue(property.Id, out var society);

                var typeOfUse = property.PropertyTypeId.HasValue && typeOfUseMap.ContainsKey(property.PropertyTypeId.Value)
                    ? typeOfUseMap[property.PropertyTypeId.Value]
                    : null;

                dynamic? propertyOld = null;
                if (newToOldIdMap.TryGetValue(property.Id, out var oldId) && propertyOldMap.ContainsKey(oldId))
                {
                    propertyOld = propertyOldMap[oldId];
                }

                var propertyType = property.PropertyTypeId.HasValue && propertyTypeMap.ContainsKey(property.PropertyTypeId.Value)
                    ? propertyTypeMap[property.PropertyTypeId.Value]
                    : null;

                taxRowsByProperty.TryGetValue(property.Id, out var taxRows);
                propertyDetailsMap.TryGetValue(property.Id, out var propertyDetail);

                var row = new Dictionary<string, object?>
                {
                    // Property fields
                    ["propertyId"] = property.Id,
                    ["propertyNo"] = property.PropertyNo,
                    ["wardId"] = property.WardId,
                    ["wardNo"] = wardNo,
                    ["partitionNo"] = property.PartitionNo,
                    ["upicId"] = property.UPICId,
                    ["subZoneNo"] = property.SubZoneNo,
                    ["mobileNo"] = property.MobileNo,
                    ["ownerTitle"] = property.OwnerTitle,
                    ["occupierTitle"] = property.OccupierTitle,
                    ["ownerName"] = property.OwnerName,
                    ["occupierName"] = property.OccupierName,
                    ["address"] = property.Address,
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
                    // Old property data (PTIS.PropertyMastOld)
                    ["oldWardNo"] = propertyOld?.OldWardNo,
                    ["oldPropertyNo"] = propertyOld?.OldPropertyNo,
                    ["oldPartitionNo"] = propertyOld?.OldPartitionNo,
                    // Property type master (PTIS.PropertyTypeMaster)
                    ["propertyDescription"] = propertyType?.PropertyDescription,
                    ["propertyType"] = propertyType?.Type,
                    ["propertyPartType"] = propertyType?.PartType,
                    // User Master (CORE.UserMaster)
                    ["userId"] = user?.Id,
                    ["userName"] = user?.UserName,
                    ["userCode"] = user?.UserCode,
                    ["userEmail"] = user?.Email,
                    ["userMobileNo"] = user?.MobileNo,
                    // ULB Master (CORE.UlbMaster)
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
                    // Property Details (PTIS.PropertyDetails — first floor entry)
                    ["floorId"] = propertyDetail?.FloorId,
                    ["subFloorId"] = propertyDetail?.SubFloorId,
                    ["constructionYear"] = propertyDetail?.ConstructionYear,
                    ["assessmentYear"] = propertyDetail?.AssessmentYear,
                    ["carpetAreaSqFeet"] = propertyDetail?.CarpetAreaSqFeet,
                    ["carpetAreaSqMeter"] = propertyDetail?.CarpetAreaSqMeter,
                    ["builtupAreaSqFeet"] = propertyDetail?.BuiltupAreaSqFeet,
                    ["builtupAreaSqMeter"] = propertyDetail?.BuiltupAreaSqMeter,
                    ["noOfRooms"] = propertyDetail?.NoOfRooms,
                    ["financeYear"] = "",
                };

                // Pivot TransMast rows into dynamic columns on the same main row.
                if (taxRows != null)
                {
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
                }

                rows.Add(row);
            }

            return rows;
        }
        // ─────────────────────────────────────────────────────────────────────────
        // Section 2 — Property Details: one row per PTIS.PropertyDetails floor entry
        //   with joined FloorMaster, ConstructionTypeMaster and TypeOfUseMaster
        // ─────────────────────────────────────────────────────────────────────────
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
                from tm in _transmastRepository.GetQueryable().Where(t => t.PropertyId == propertyId)
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

                row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
            }

            return new List<object> { row };
        }
    }
}
