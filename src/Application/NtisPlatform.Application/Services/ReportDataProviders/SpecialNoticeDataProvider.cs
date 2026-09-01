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
    /// SpecialNotice report data provider.
    ///
    /// Produces two logical sections per certificate call:
    ///   "main"       — paged properties + ward + society + type-of-use fields + dynamic pivoted taxes.
    ///   "taxDetails" — paged properties pivoted with TaxName and dynamic tax columns.
    /// </summary>
    public class SpecialNoticeDataProvider : IPagedReportDataProvider
    {
        public const string MainSection = "main";
        public const string TaxDetailsSection = "taxDetails";

        public string ProviderCode => "SpecialNoticeDataProvider";

        private static readonly List<string> ActiveTaxCodes = new()
        {
            "GEN",
            "STATE_EDU",
            "STATE_EMP",
            "TREE",
            "SP_WATER",
            "ROAD",
            "FIRE",
            "LIGHT",
            "WATER_BEN",
            "SEWAGE",
            "SP_EDU"
        };

        private readonly IReportDataRepository<PropertyEntity> _propertyRepository;
        private readonly IReportDataRepository<WardEntity> _wardRepository;
        private readonly IReportDataRepository<SocietyDetailsEntity> _societyRepository;
        private readonly IReportDataRepository<TypeOfUseEntity> _typeOfUseRepository;
        private readonly IReportDataRepository<TransMastEntity> _transmastRepository;
        private readonly IReportDataRepository<TaxMasterEntity> _taxMastRepository;
        private readonly IReportDataRepository<ULBMasterEntity> _ulbMasterRepository;
        private readonly IReportDataRepository<UserEntity> _userRepository;
        private readonly IReportDataRepository<RVCalculationResultsEntity> _rvResultsRepository;
        private readonly IReportDataRepository<ZoneEntity> _zoneRepository;
        private readonly IReportDataRepository<PropertyTypeMasterEntity> _propertyTypeMasterRepository;
        private readonly IReportingRepository<ReportRequestEntity, Guid> _reportRequestRepository;
        private readonly IReportDataRepository<YearMasterEntity> _yearRepository;

        public SpecialNoticeDataProvider(
            IReportDataRepository<PropertyEntity> propertyRepository,
            IReportDataRepository<WardEntity> wardRepository,
            IReportDataRepository<SocietyDetailsEntity> societyRepository,
            IReportDataRepository<TypeOfUseEntity> typeOfUseRepository,
            IReportDataRepository<TransMastEntity> transmastRepository,
            IReportDataRepository<TaxMasterEntity> taxMastRepository,
            IReportDataRepository<ULBMasterEntity> ulbMasterRepository,
            IReportDataRepository<UserEntity> userRepository,
            IReportDataRepository<RVCalculationResultsEntity> rvResultsRepository,
            IReportDataRepository<ZoneEntity> zoneRepository,
            IReportDataRepository<PropertyTypeMasterEntity> propertyTypeMasterRepository,
            IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
            IReportDataRepository<YearMasterEntity> yearRepository)
        {
            _propertyRepository = propertyRepository;
            _wardRepository = wardRepository;
            _societyRepository = societyRepository;
            _typeOfUseRepository = typeOfUseRepository;
            _transmastRepository = transmastRepository;
            _taxMastRepository = taxMastRepository;
            _ulbMasterRepository = ulbMasterRepository;
            _userRepository = userRepository;
            _rvResultsRepository = rvResultsRepository;
            _zoneRepository = zoneRepository;
            _propertyTypeMasterRepository = propertyTypeMasterRepository;
            _reportRequestRepository = reportRequestRepository;
            _yearRepository = yearRepository;
        }

        public IReadOnlyList<ReportSectionDescriptor> GetSections() => new[]
        {
            new ReportSectionDescriptor(MainSection,       false),
            new ReportSectionDescriptor(TaxDetailsSection, false),
        };

        public async Task<object> GetDataAsync(
            Dictionary<string, string> parameters, CancellationToken ct = default)
        {
            var (rows, _) = await BuildPageAsync(Guid.Empty, parameters, MainSection, skip: 0, take: int.MaxValue, ct);
            return rows;
        }

        public async Task<ReportDataPage> GetDataPageAsync(Guid reportRequestId,
            Dictionary<string, string> parameters, string section, int page, int pageSize, CancellationToken ct = default)
        {
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

        private async Task<(List<object> Rows, bool HasMore)> BuildPageAsync(
            Guid reportRequestId, Dictionary<string, string> parameters, string section, int skip, int take, CancellationToken ct)
        {
            // --- Parse parameters ---
            parameters.TryGetValue("ownerId", out var ownerIdText);
            parameters.TryGetValue("propertyId", out var propertyIdText);

            var ownerIds = new List<int>();
            if (!string.IsNullOrWhiteSpace(ownerIdText))
            {
                ownerIds.AddRange(ownerIdText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0));
            }
            if (!string.IsNullOrWhiteSpace(propertyIdText))
            {
                ownerIds.AddRange(propertyIdText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0));
            }
            ownerIds = ownerIds.Distinct().ToList();

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

            // helper: case-insensitive parameter lookup
            static string? GetParam(IDictionary<string, string> p, string key)
            {
                if (p.TryGetValue(key, out var v)) return string.IsNullOrWhiteSpace(v) ? null : v;
                var kv = p.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                return string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value;
            }

            var type = GetParam(parameters, "Type")?.Trim().ToUpper();

            var propertyTypeIdText = GetParam(parameters, "propertyTypeId");
            int.TryParse(propertyTypeIdText, out var propertyTypeId);

            var propertyDescription = GetParam(parameters, "PropertyDescription")?.Trim();

            var financeYear = ParseFinanceYear(parameters);
            var activeYearId = await BaseQuery(financeYear).Select(x => x.Id).FirstOrDefaultAsync(ct);

            // ---------------- GET USER INFO ----------------
            var requestedByUserId = await _reportRequestRepository.GetQueryable()
                .Where(r => r.ReportRequestId == reportRequestId)
                .Select(r => (int?)r.RequestedByUserId)
                .FirstOrDefaultAsync(ct);

            parameters.TryGetValue("userId", out var userIdStr);
            int.TryParse(userIdStr, out var userId);
            var finalUserId = userId > 0 ? userId : (requestedByUserId ?? 0);

            var user = finalUserId <= 0
                ? null
                : await _userRepository.GetQueryable()
                    .Where(u => u.Id == finalUserId)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.FirstName,
                        u.MiddleName,
                        u.LastName,
                        u.UserCode,
                        u.Email,
                        u.MobileNo
                    })
                    .FirstOrDefaultAsync(ct);

            // ---------------- PROPERTY QUERY ----------------
            var propQuery =
                from pm in _propertyRepository.GetQueryable()
                join pt in _propertyTypeMasterRepository.GetQueryable() on pm.PropertyTypeId equals pt.Id into ptj
                from pt in ptj.DefaultIfEmpty()
                join wm in _wardRepository.GetQueryable() on pm.WardId equals wm.Id into wmj
                from wm in wmj.DefaultIfEmpty()
                join zm in _zoneRepository.GetQueryable() on wm.ZoneId equals zm.Id into zmj
                from zm in zmj.DefaultIfEmpty()

                where pm.IsActive && !pm.MarkedForDeletion
                      && (ownerIds.Count == 0 || ownerIds.Contains(pm.Id))
                      && (zoneId == 0 || (wm != null && wm.ZoneId == zoneId))
                      && (wardId == 0 || pm.WardId == wardId)
                      && (propertyNoText == null || pm.PropertyNo == propertyNoText)
                      && (partitionNoText == null || pm.PartitionNo == partitionNoText)
                      && (assessmentStatus == 0 || pm.PropertyAssessmentStatusId == assessmentStatus)
                      && (string.IsNullOrEmpty(type) || (pt != null && pt.Type == type))
                      && (propertyTypeId == 0 || (pt != null && pt.Id == propertyTypeId))
                      && (string.IsNullOrEmpty(propertyDescription) || (pt != null && pt.PropertyDescription == propertyDescription))
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
                    PropertyDescription = pt != null ? pt.PropertyDescription : null,
                    pm.PlotNo,
                    WardNo = wm != null ? wm.WardNo : null,
                    ZoneNo = zm != null ? zm.ZoneNo : null,
                };

            // Apply finance-year constraint server-side when caller provided a financeYear
            if (financeYear != 0 && activeYearId > 0)
            {
                var transQ = _transmastRepository.GetQueryable()
                    .Where(t => t.FinanceYearId == activeYearId && !t.MarkedForDeletion && t.IsActive)
                    .Select(t => t.PropertyId);
                propQuery = propQuery.Where(x => transQ.Contains(x.Id));
            }

            var propsList = await propQuery
                .Distinct()
                .OrderBy(x => x.PropertyNo)
                .ThenBy(x => x.PartitionNo)
                .ToListAsync(ct);

            // Apply FromPropertyNo & ToPropertyNo range filters
            if (int.TryParse(fromPropertyNoText, out var fromPropNo))
            {
                propsList = propsList
                    .Where(x => int.TryParse(x.PropertyNo, out var no) && no >= fromPropNo)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(fromPropertyNoText))
            {
                propsList = propsList
                    .Where(x => string.Compare(x.PropertyNo, fromPropertyNoText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            if (int.TryParse(toPropertyNoText, out var toPropNo))
            {
                propsList = propsList
                    .Where(x => int.TryParse(x.PropertyNo, out var no) && no <= toPropNo)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(toPropertyNoText))
            {
                propsList = propsList
                    .Where(x => string.Compare(x.PropertyNo, toPropertyNoText, StringComparison.OrdinalIgnoreCase) <= 0)
                    .ToList();
            }

            // Pagination
            var takePlusOne = take == int.MaxValue ? int.MaxValue : take + 1;
            var pagedProps = propsList.Skip(skip).Take(takePlusOne).ToList();
            var hasMore = take != int.MaxValue && pagedProps.Count > take;
            if (hasMore) pagedProps = pagedProps.Take(take).ToList();

            if (!pagedProps.Any())
            {
                return (new List<object>(), false);
            }

            var pagedPropertyIds = pagedProps.Select(p => p.Id).ToList();

            // 1b. Society details (LEFT JOIN on PropertyId)
            var societies = await _societyRepository.GetQueryable()
                .Where(sd => sd.PropertyId.HasValue && pagedPropertyIds.Contains(sd.PropertyId.Value))
                .Select(sd => new
                {
                    PropertyId = sd.PropertyId.Value,
                    sd.WingId,
                    sd.WingName,
                    sd.SocietyName,
                    sd.SocietyAddress,
                })
                .ToListAsync(ct);
            var societyMap = societies.GroupBy(s => s.PropertyId).ToDictionary(g => g.Key, g => g.First());

            // 1c. Type-of-use (LEFT JOIN on PM.PropertyTypeId = TUM.Id)
            var propertyTypeIds = pagedProps.Where(p => p.PropertyTypeId.HasValue).Select(p => p.PropertyTypeId!.Value).Distinct().ToList();
            var typeOfUses = await _typeOfUseRepository.GetQueryable()
                .Where(t => propertyTypeIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    t.Description,
                    t.TypeOfUseCode,
                })
                .ToListAsync(ct);
            var typeOfUseMap = typeOfUses.ToDictionary(t => t.Id);

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

            // 1f. TransMast pivot — fetch all tax lines for paged properties, filtering by activeYearId
            var taxRows = await (
                from tm in _transmastRepository.GetQueryable().Where(t => pagedPropertyIds.Contains(t.PropertyId))
                join tam in _taxMastRepository.GetQueryable() on tm.TaxId equals tam.Id
                where tm.FinanceYearId == activeYearId && tm.IsActive && !tm.MarkedForDeletion && tam.TaxCode != "TaxTotal"
                orderby tam.DisplayOrder
                select new
                {
                    tm.PropertyId,
                    tam.TaxCode,
                    tam.TaxName,
                    RVorCV = tm.CalculationType,
                    RVorCVValue = tm.CalculationValue,
                    tm.TaxAmount
                }
            ).ToListAsync(ct);
            var taxRowsMap = taxRows.GroupBy(t => t.PropertyId).ToDictionary(g => g.Key, g => g.ToList());

            // No custom allSafeTaxCodes lookup list needed, as we strictly output only the 11 active tax codes.

            // RV Calculation results for aggregates (varshik bhade mulya & durusti)
            var rvResults = await _rvResultsRepository.GetQueryable()
                .Where(r => pagedPropertyIds.Contains(r.PropertyId) && r.IsActive && !r.MarkedForDeletion)
                .Select(r => new
                {
                    r.PropertyId,
                    r.AnnualRentalValue,
                    r.Maintenance,
                    r.RateableValue
                })
                .ToListAsync(ct);
            var rvResultsMap = rvResults.GroupBy(r => r.PropertyId).ToDictionary(g => g.Key, g => g.ToList());

            var rows = new List<object>();

            foreach (var property in pagedProps)
            {
                societyMap.TryGetValue(property.Id, out var society);
                var typeOfUse = property.PropertyTypeId.HasValue && typeOfUseMap.TryGetValue(property.PropertyTypeId.Value, out var tou) ? tou : null;
                taxRowsMap.TryGetValue(property.Id, out var propTaxRows);
                rvResultsMap.TryGetValue(property.Id, out var propRvResults);

                double totalAnnualRentalValue = 0;
                decimal totalMaintenance = 0;
                decimal totalRateableValue = 0;

                if (propRvResults != null && propRvResults.Any())
                {
                    totalAnnualRentalValue = propRvResults.Sum(r => (double)(r.AnnualRentalValue ?? 0d));
                    totalMaintenance = propRvResults.Sum(r => (decimal)(r.Maintenance ?? 0m));
                    totalRateableValue = propRvResults.Sum(r => (decimal)(r.RateableValue ?? 0m));
                }

                var propTaxes = propTaxRows != null ? propTaxRows.Cast<dynamic>().ToList() : new List<dynamic>();

                var hasRv = propTaxes.Any(t => string.Equals((string)t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase));
                var hasCv = propTaxes.Any(t => string.Equals((string)t.RVorCV, "CV", StringComparison.OrdinalIgnoreCase));

                if (hasCv && !hasRv)
                {
                    continue;
                }

                string activeCalcType = "RV";
                if (hasRv && hasCv)
                {
                    activeCalcType = "RCV";
                }
                else if (hasRv)
                {
                    propTaxes = propTaxes.Where(t => string.Equals((string)t.RVorCV, "RV", StringComparison.OrdinalIgnoreCase)).ToList();
                    activeCalcType = "RV";
                }


                // Filter and deduplicate tax rows
                var uniqueTaxes = propTaxes
                    .GroupBy(t => !string.IsNullOrWhiteSpace((string)t.TaxCode) ? (string)t.TaxCode : (string)t.TaxName ?? "UNKNOWN")
                    .Select(g => g.First())
                    .ToList();

                // Calculate total tax
                decimal totalTax = uniqueTaxes.Sum(t => (decimal)t.TaxAmount);
                Dictionary<string, object?> row;

                if (section.Equals(TaxDetailsSection, StringComparison.OrdinalIgnoreCase))
                {
                    row = new Dictionary<string, object?>
                    {
                        ["propertyId"] = property.Id,
                        ["calculationAnnualValue"] = totalAnnualRentalValue,
                        ["annualRentalValue"] = totalAnnualRentalValue,
                        ["AnnualRentalValue"] = totalAnnualRentalValue,
                        ["maintenance"] = totalMaintenance,
                        ["Maintenance"] = totalMaintenance,
                        ["durusti"] = totalMaintenance,
                        ["rateableValue"] = totalRateableValue,
                        ["totalTax"] = totalTax,
                        ["TotalTax"] = totalTax,
                        ["ekunKar"] = totalTax,
                    };
                }
                else // MainSection
                {
                    row = new Dictionary<string, object?>
                    {
                        ["propertyId"] = property.Id,
                        ["propertyNo"] = property.PropertyNo,
                        ["zoneNo"] = property.ZoneNo,
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
                        ["plotNo"] = property.PlotNo,
                        ["PlotNo"] = property.PlotNo,
                        // Society details
                        ["wingId"] = society?.WingId,
                        ["wingName"] = society?.WingName,
                        ["societyName"] = society?.SocietyName,
                        ["societyAddress"] = society?.SocietyAddress,
                        // Type-of-use
                        ["typeOfUseDesc"] = typeOfUse?.Description,
                        ["Description"] = property.PropertyDescription,
                        ["typeOfUseCode"] = typeOfUse?.TypeOfUseCode,
                        // User Master fields (CORE.UserMaster)
                        ["userId"] = user?.Id,
                        ["userName"] = user?.UserName,
                        ["firstName"] = user?.FirstName,
                        ["middleName"] = user?.MiddleName,
                        ["lastName"] = user?.LastName,
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
                        // RV Calculation properties
                        ["calculationAnnualValue"] = totalAnnualRentalValue,
                        ["annualRentalValue"] = totalAnnualRentalValue,
                        ["AnnualRentalValue"] = totalAnnualRentalValue,
                        ["maintenance"] = totalMaintenance,
                        ["Maintenance"] = totalMaintenance,
                        ["durusti"] = totalMaintenance,
                        ["rateableValue"] = totalRateableValue,
                        ["totalTax"] = totalTax,
                        ["TotalTax"] = totalTax,
                        ["ekunKar"] = totalTax,
                    };
                }

                // Pre-populate only the 11 active tax codes with default 0/activeCalcType/0m values
                foreach (var code in ActiveTaxCodes)
                {
                    row[$"Transmast_{code}"] = 0m;
                    row[$"RVorCV_{code}"] = activeCalcType;
                    row[$"RVorCVValue_{code}"] = 0m;
                }

                // Pivot TransMast rows into dynamic columns
                foreach (var tax in uniqueTaxes)
                {
                    var safeCode = GetSafeTaxCode((string?)tax.TaxCode, (string?)tax.TaxName);
                    if (ActiveTaxCodes.Contains(safeCode))
                    {
                        row[$"Transmast_{safeCode}"] = tax.TaxAmount;
                        row[$"RVorCV_{safeCode}"] = tax.RVorCV;
                        row[$"RVorCVValue_{safeCode}"] = tax.RVorCVValue;
                        row[$"CalculationAnnualValue_{safeCode}"] = totalAnnualRentalValue;
                        row[$"Maintenance_{safeCode}"] = totalMaintenance;
                    }
                }

                rows.Add(row);
            }

            return (rows, hasMore);
        }

        private static string GetSafeTaxCode(string? taxCode, string? taxName)
        {
            var rawCode = !string.IsNullOrWhiteSpace(taxCode) ? taxCode : taxName ?? "UNKNOWN";
            return rawCode.Replace("SP_E DU", "SP_EDU").Replace(' ', '_');
        }
    }
}
