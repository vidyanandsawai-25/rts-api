using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;
using System;

namespace NtisPlatform.Application.Services.ReportDataProviders
{
    /// <summary>
    /// TransferCertificate report data provider.
    /// 
    /// Produces one logical section per call:
    ///   "main" — one row: property master + ward + society + type-of-use + ULB + user
    ///            + OldWardNo/OldPropertyNo/OldPartitionNo from PTIS.PropertyMastOld
    ///            + PropertyDescription/Type/PartType from PTIS.PropertyTypeMaster
    ///            + pivoted TransMast tax columns.
    /// 
    /// Join conditions:
    ///   PTIS.PropertyMast.PropertyMastOldId = PTIS.PropertyMastOld.Id
    ///   PTIS.PropertyMast.PropertyTypeId    = PTIS.PropertyTypeMaster.Id
    /// 
    /// Section discovery is static (no query runs during authenticate).
    /// </summary>
    public class TransferCertificateDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";

        public string ProviderCode => "TransferCertificateDataProvider";

        private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
        private readonly IReportDataRepository<WardEntity> _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity> _typeOfUseRepository;
        private readonly IReportDataRepository<PropertyMastOldEntity> _propertyMastOldRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeRepository;
        private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity> _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity> _userRepository;
        private readonly IReportDataRepository<YearMasterEntity> _yearRepository;
        private readonly IReportDataRepository<TransMastEntity> _transRepository;
        private readonly IReportingRepository<ReportRequestEntity, Guid> _ReportRequestRepository;
        private readonly IReportDataRepository<PropertyMapDetailEntity> _propertyMapDetailRepository;

        public TransferCertificateDataProvider(
            IReportDataRepository<PropertyEntity> propertyRepository,
            IReportDataRepository<WardEntity> wardRepository,
            IReportDataRepository<SocietyDetailsEntity> societyRepository,
            IReportDataRepository<TypeOfUseEntity> typeOfUseRepository,
            IReportDataRepository<PropertyMastOldEntity> propertyMastOldRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeRepository,
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
            new ReportSectionDescriptor(MainSection, false),
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
        private static short ParseFinanceYear(Dictionary<string, string> parameters)
        {
            parameters.TryGetValue("financeYear", out var financeYearStr);
            short.TryParse(financeYearStr, out var financeYear);
            return financeYear;
        }
        private IQueryable<YearMasterEntity> BaseQuery(short financeYear) => _yearRepository.GetQueryable()
            .Where(b => financeYear == 0 ? b.IsActive : b.Year == financeYear);

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Guid reportRequestId,
            Dictionary<string, string> parameters, int skip, int take, CancellationToken ct)
        {
            parameters.TryGetValue("ownerId", out var ownerIdText);

            var ownerIds = string.IsNullOrWhiteSpace(ownerIdText) ? new List<int>() : ownerIdText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

            var financeYear = ParseFinanceYear(parameters);
            int activeYearId = 0;
            if (financeYear != 0)
            {
                activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);
            }

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


            parameters.TryGetValue("Type", out var type);
            type = string.IsNullOrWhiteSpace(type)
                ? null
                : type.Trim().ToUpper();

            parameters.TryGetValue("propertyTypeId", out var propertyTypeIdText);
            int.TryParse(propertyTypeIdText, out var propertyTypeId);

            parameters.TryGetValue("PropertyDescription", out var propertyDescription);
            propertyDescription = string.IsNullOrWhiteSpace(propertyDescription) ? null : propertyDescription.Trim();


            // propertyId accepts a single value OR comma-separated list: "101,202,303" propertyid means owenerid
            parameters.TryGetValue("propertyId", out var propertyIdStr);
            parameters.TryGetValue("userId", out var userIdStr);

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
                    join pt in _propertyTypeRepository.GetQueryable() on p.PropertyTypeId equals pt.Id into ptj
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

                propertyIds = await query.Select(p => p.Id).Distinct().ToListAsync(ct);
            }

            var rows = await BuildMainRowsAsync(propertyIds, reportRequestId, ct);

            // Apply skip/take.
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

            // 1a. Properties + Wards
            var properties = await (
                from pm in _propertyRepository.GetQueryable().Where(p => propertyIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
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
                    assessmentStatus = pm.PropertyAssessmentStatusId,
                }
            ).Distinct().ToListAsync(ct);

            if (!properties.Any())
                return new List<object>();

            // 1b. Society details map
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

            var societyMap = societyDetails.GroupBy(s => s.PropertyId)
                                           .ToDictionary(g => g.Key, g => g.First());

            // 1c. Unique type-of-use IDs -> TypeOfUse map
            var uniqueTypeIds = properties
                .Select(p => p.PropertyTypeId)
                .Where(tid => tid.HasValue && tid.Value > 0)
                .Select(tid => tid!.Value)
                .Distinct()
                .ToList();

            var typeOfUseMap = new Dictionary<int, dynamic>();
            foreach (var tid in uniqueTypeIds)
            {
                var typeOfUse = await _typeOfUseRepository.GetQueryable()
                    .Where(t => t.Id == tid)
                    .Select(t => new
                    {
                        t.Description,
                        t.TypeOfUseCode,
                    })
                    .FirstOrDefaultAsync(ct);
                if (typeOfUse != null)
                {
                    typeOfUseMap[tid] = typeOfUse;
                }
            }

            // 1d. Map new property IDs to old property IDs via PropertyMapDetail
            var propertyMappings = await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd => pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value) && pmd.IsActive && pmd.IsCurrent && pmd.Status == "ACTIVE")
                .Select(pmd => new { pmd.PropertyIdNew, pmd.PropertyIdOld })
                .ToListAsync(ct);

            var newToOldIdMap = propertyMappings
                .Where(m => m.PropertyIdNew.HasValue && m.PropertyIdOld.HasValue)
                .GroupBy(m => m.PropertyIdNew!.Value)
                .ToDictionary(g => g.Key, g => g.First().PropertyIdOld!.Value);

            var uniqueOldIds = newToOldIdMap.Values.Distinct().ToList();

            var oldMap = new Dictionary<int, dynamic>();
            foreach (var oid in uniqueOldIds)
            {
                var oldObj = await _propertyMastOldRepository.GetQueryable()
                    .Where(o => o.Id == oid)
                    .Select(o => new
                    {
                        o.OldWardNo,
                        o.OldPropertyNo,
                        o.OldPartitionNo,
                    })
                    .FirstOrDefaultAsync(ct);
                if (oldObj != null)
                {
                    oldMap[oid] = oldObj;
                }
            }

            // 1e. Unique PropertyTypeId -> PropertyType map
            var propertyTypeMap = new Dictionary<int, dynamic>();
            foreach (var tid in uniqueTypeIds)
            {
                var propertyType = await _propertyTypeRepository.GetQueryable()
                    .Where(pt => pt.Id == tid)
                    .Select(pt => new
                    {
                        pt.PropertyDescription,
                        pt.Type,
                        pt.PartType,
                    })
                    .FirstOrDefaultAsync(ct);
                if (propertyType != null)
                {
                    propertyTypeMap[tid] = propertyType;
                }
            }

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

            // 1g. User (single row)
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

            // 1h. TransMast pivot
            var taxRowsAll = await (
                from tm in _transmastRepository.GetQueryable().Where(t => propertyIds.Contains(t.PropertyId))
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

            var allRows = new List<object>();

            foreach (var property in properties)
            {
                var society = societyMap.ContainsKey(property.Id) ? societyMap[property.Id] : null;

                var typeOfUse = property.PropertyTypeId.HasValue && typeOfUseMap.ContainsKey(property.PropertyTypeId.Value)
                    ? typeOfUseMap[property.PropertyTypeId.Value]
                    : null;

                dynamic? propertyOld = null;
                if (newToOldIdMap.TryGetValue(property.Id, out var oldId) && oldMap.ContainsKey(oldId))
                {
                    propertyOld = oldMap[oldId];
                }

                var propertyType = property.PropertyTypeId.HasValue && propertyTypeMap.ContainsKey(property.PropertyTypeId.Value)
                    ? propertyTypeMap[property.PropertyTypeId.Value]
                    : null;

                System.Collections.IEnumerable taxRows = Array.Empty<object>();
                if (taxRowsByProperty.ContainsKey(property.Id))
                {
                    taxRows = taxRowsByProperty[property.Id];
                }

                var row = new Dictionary<string, object?>
                {
                    // Property fields
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
                    ["flatOrShopNo"] = property.FlatOrShopNo,
                    ["flatOrShopName"] = property.FlatOrShopName,
                    // Society details
                    ["wingId"] = society?.WingId,
                    ["wingName"] = society?.WingName,
                    ["societyName"] = society?.SocietyName,
                    ["societyAddress"] = society?.SocietyAddress,
                    // Type-of-use (TypeOfUseMaster)
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
                    ["emailId"] = ulb?.EmailId,
                    ["ulbMobileNo"] = ulb?.MobileNo,
                    ["mobileNo"] = ulb?.MobileNo,
                    ["ulbAlternateMobileNo"] = ulb?.AlternateMobileNo,
                    ["ulbWebsiteUrl"] = ulb?.WebsiteUrl,
                    ["ulbAddress"] = ulb?.UlbAddress,
                    ["ulbState"] = ulb?.State,
                    ["ulbDistrict"] = ulb?.District,
                    ["ulbPinCode"] = ulb?.PinCode,
                    ["financeYear"] = "",
                };

                // Pivot TransMast rows into dynamic columns on the same main row.
                foreach (dynamic tax in taxRows)
                {
                    string? taxCode = tax.TaxCode;
                    string? taxName = tax.TaxName;
                    string safeCode = (!string.IsNullOrWhiteSpace(taxCode) ? taxCode : (!string.IsNullOrWhiteSpace(taxName) ? taxName : "UNKNOWN")).Replace(' ', '_');

                    row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                    row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                    row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
                }

                allRows.Add(row);
            }

            return allRows;
        }
    }
}
