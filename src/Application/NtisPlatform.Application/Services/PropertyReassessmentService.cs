using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyReassessment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Re-implements the legacy "Property Re-Assessment" SQL script in application code, now with support
/// for PropertyMapMaster/PropertyMapDetail mappings (ONE_TO_ONE, SPLIT, MERGE, MAP scenarios).
/// Read-only: resolves the property from Ward + PropertyNo (+ optional PartitionNo), discovers all
/// mapped old properties via the mapping tables, then assembles old/new photos (STEP 2),
/// old/new floor details (STEP 3) and the old-vs-new tax-head summary (STEP 4).
///
/// Per the repo convention (see <see cref="DataEntrySameAsService"/> and PropertyOldDetailsRepository),
/// everything is EF Core LINQ over <see cref="IRepository{T,TKey}"/> — no raw SQL. The dynamic PIVOT
/// of STEP 4 is replaced by an in-memory tax-head projection.
/// </summary>
public class PropertyReassessmentService : IPropertyReassessmentService
{
    private const string PlanPhotoCode = "PLAN_PHOTO";
    private const string PropertyPhotoCode = "PROPERTY_PHOTO";

    // Certificate type codes (PropertyCertificateTypeMaster.CertificateTypeCode) relevant to reassessment
    // Format: comma-separated values (e.g., "OC" or "OC,CC")
    // Change this string to modify which certificate types are included — no other code changes needed
    private const string PropertyReassessmentCertificateTypeCode = "OC,CC";

    // Derived set of string codes to match against database values
    private static readonly HashSet<string> ReassessmentCertificateTypeCodes =
        PropertyReassessmentCertificateTypeCode.Split(',').Select(c => c.Trim()).ToHashSet();

    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyDetailsOldEntity, int> _propertyDetailsOldRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyPhotoEntity, int> _propertyPhotoRepository;
    private readonly IRepository<PropertyPhotoOldEntity, int> _propertyPhotoOldRepository;
    private readonly IRepository<PropertyPhotoTypeEntity, int> _propertyPhotoTypeRepository;
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<DocumentBindingEntity, int> _documentBindingRepository;
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<RenterMastEntity, int> _renterRepository;
    private readonly IRepository<RVCalculationResultsEntity, int> _rvResultsRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<TransMastOldEntity, int> _transMastOldRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyCertificateEntity, int> _propertyCertificateRepository;

    public PropertyReassessmentService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyDetailsOldEntity, int> propertyDetailsOldRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<PropertyPhotoEntity, int> propertyPhotoRepository,
        IRepository<PropertyPhotoOldEntity, int> propertyPhotoOldRepository,
        IRepository<PropertyPhotoTypeEntity, int> propertyPhotoTypeRepository,
        IRepository<DocumentEntity, int> documentRepository,
        IRepository<DocumentBindingEntity, int> documentBindingRepository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<ConstructionTypeEntity, int> constructionTypeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<RenterMastEntity, int> renterRepository,
        IRepository<RVCalculationResultsEntity, int> rvResultsRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<TransMastOldEntity, int> transMastOldRepository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyCertificateEntity, int> propertyCertificateRepository)
    {
        _propertyRepository = propertyRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyDetailsOldRepository = propertyDetailsOldRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyPhotoRepository = propertyPhotoRepository;
        _propertyPhotoOldRepository = propertyPhotoOldRepository;
        _propertyPhotoTypeRepository = propertyPhotoTypeRepository;
        _documentRepository = documentRepository;
        _documentBindingRepository = documentBindingRepository;
        _floorRepository = floorRepository;
        _constructionTypeRepository = constructionTypeRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _renterRepository = renterRepository;
        _rvResultsRepository = rvResultsRepository;
        _transMastRepository = transMastRepository;
        _transMastOldRepository = transMastOldRepository;
        _taxMasterRepository = taxMasterRepository;
        _yearMasterRepository = yearMasterRepository;
        _propertyMapMasterRepository = propertyMapMasterRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyCertificateRepository = propertyCertificateRepository;
    }

    public async Task<PropertyReassessmentDto> GetReassessmentAsync(
        PropertyReassessmentQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        // ── STEP 1: resolve the single new property ────────────────────────────
        var propertyNo = query.PropertyNo;
        var partitionNo = query.PartitionNo;  // Don't trim; use ISNULL logic per spec

        var matches = await _propertyRepository.GetQueryable()
            .Where(p => p.WardId == query.WardId
                        && p.PropertyNo == propertyNo
                        && (string.IsNullOrEmpty(partitionNo) ? (p.PartitionNo == null || p.PartitionNo == "") : p.PartitionNo == partitionNo)
                        && p.IsActive
                        && !p.MarkedForDeletion)
            .Select(p => new { p.Id })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
            throw new ArgumentException("No property found for the supplied Ward, Property No and Partition No.");

        if (matches.Count > 1)
            throw new ArgumentException(
                "More than one property matches the supplied Ward and Property No. Please specify a Partition No.");

        var propertyId = matches[0].Id;

        // ── STEP 2: resolve old properties via PropertyMapMaster/PropertyMapDetail ────────
        var (oldPropertyIds, siblingNewPropertyIds, mappings) =
            await ResolveMappingAsync(propertyId, cancellationToken);

        var result = new PropertyReassessmentDto
        {
            PropertyId = propertyId
        };

        // ── STEP 3: photos (new + old if mapped) ──────────────────────────────
        result.Photos = await GetPhotosAsync(propertyId, oldPropertyIds, cancellationToken);

        // ── Get certificates (CC/OC) for new properties only ────────────────
        var (ocCerts, ccCerts) = await GetCertificatesAsync(propertyId, cancellationToken);

        // ── STEP 4: floor details (new + old from all mapped old properties) ───
        result.NewFloorDetails = await GetNewFloorDetailsAsync(propertyId, ocCerts, ccCerts, cancellationToken);
        result.OldFloorDetails = oldPropertyIds.Count > 0
            ? await GetOldFloorDetailsAsync(oldPropertyIds, cancellationToken)
            : [];

        ApplyFloorChangeStatus(result.NewFloorDetails, result.OldFloorDetails);

        // ── STEP 5: tax-head summary (old aggregated across all mapped properties) ──
        result.TaxSummary = await GetTaxSummaryAsync(propertyId, oldPropertyIds, cancellationToken);

        return result;
    }

    /// <summary>
    /// STEP 2 — Resolve mapping group: two-step query to fetch the entire mapping family.
    /// STEP 2.1: Find PropertyMapIds touching this property (where PropertyIdNew == propertyId).
    /// STEP 2.2: Fetch ALL rows in those mapping groups (no PropertyIdNew filter) to capture siblings in SPLIT scenarios.
    /// </summary>
    private async Task<(List<int> OldPropertyIds, List<int> SiblingNewPropertyIds, List<PropertyMappingDto> Mappings)>
        ResolveMappingAsync(int propertyId, CancellationToken cancellationToken)
    {
        // STEP 2.1: Find PropertyMapMasterIds linked to this new property
        var mapIds = await _propertyMapDetailRepository.GetQueryable()
            .Where(pmd => pmd.PropertyIdNew == propertyId
                         && pmd.IsActive && pmd.IsCurrent && pmd.Status == "ACTIVE")
            .Select(pmd => pmd.PropertyMapId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Early exit if no mapping exists
        if (mapIds.Count == 0)
            return ([], [], []);

        // STEP 2.2: Fetch ALL rows in those PropertyMapMasterIds (no PropertyIdNew filter, so siblings are included)
        var mappings = await (
            from pmd in _propertyMapDetailRepository.GetQueryable()
            join pmm in _propertyMapMasterRepository.GetQueryable() on pmd.PropertyMapId equals pmm.Id
            //where mapIds.Contains(pmd.PropertyMapId)
            where pmd.PropertyIdNew == propertyId
                  && pmd.IsActive && pmd.IsCurrent && pmd.Status == "ACTIVE"
                  && pmm.IsActive
            select new PropertyMappingDto
            {
                PropertyMapId = pmm.Id,
                MappingCategory = pmm.MappingCategory,
                VersionNo = pmm.VersionNo,
                PropertyIdOld = pmd.PropertyIdOld,
                PropertyIdNew = pmd.PropertyIdNew,
                PropertyNo = string.Empty,
                TaxSharePercent = pmd.TaxSharePercent,
                AreaSharePercent = pmd.AreaSharePercent,
                Status = pmd.Status
            }
        ).ToListAsync(cancellationToken);

        var oldPropertyIds = mappings
            .Where(m => m.PropertyIdOld.HasValue)
            .Select(m => m.PropertyIdOld!.Value)
            .Distinct()
            .ToList();

        var siblingNewPropertyIds = mappings
            .Where(m => m.PropertyIdNew.HasValue && m.PropertyIdNew.Value != propertyId)
            .Select(m => m.PropertyIdNew!.Value)
            .Distinct()
            .ToList();

        return (oldPropertyIds, siblingNewPropertyIds, mappings);
    }

    /// <summary>Fetch per-floor certificates for new properties only, separated by type (OC/CC). Per-PropertyDetailsId lookup. Certificate types matched from ReassessmentCertificateTypeCodes (configured via PropertyReassessmentCertificateTypeCode string constant).</summary>
    private async Task<(Dictionary<(int PropertyId, int? DetailId), (string? CertNo, DateTime? IssueDate)> OCCerts, Dictionary<(int PropertyId, int? DetailId), (string? CertNo, DateTime? IssueDate)> CCCerts)> GetCertificatesAsync(
        int propertyId,
        CancellationToken cancellationToken)
    {
        var certRows = await _propertyCertificateRepository.GetQueryable()
            .Include(pc => pc.CertificateType)
            .Where(pc => pc.PropertyId == propertyId
                         && pc.IsActive && !pc.MarkedForDeletion
                         && pc.PropertyDetailsId.HasValue
                         && pc.CertificateType != null
                         && ReassessmentCertificateTypeCodes.Contains(pc.CertificateType.CertificateTypeCode)
                         && pc.CertificateType.IsActive)
            .Select(pc => new { pc.PropertyId, pc.PropertyDetailsId, pc.CertificateNo, pc.IssueDate, CertificateTypeCode = pc.CertificateType!.CertificateTypeCode })
            .ToListAsync(cancellationToken);

        // Separate into OC and CC dictionaries
        var ocCerts = certRows
            .Where(c => c.CertificateTypeCode == "OC")
            .GroupBy(c => (c.PropertyId, c.PropertyDetailsId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var r = g.OrderByDescending(x => x.IssueDate ?? DateTime.MinValue).First();
                    return (r.CertificateNo, r.IssueDate);
                });

        var ccCerts = certRows
            .Where(c => c.CertificateTypeCode == "CC")
            .GroupBy(c => (c.PropertyId, c.PropertyDetailsId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var r = g.OrderByDescending(x => x.IssueDate ?? DateTime.MinValue).First();
                    return (r.CertificateNo, r.IssueDate);
                });

        return (ocCerts, ccCerts);
    }

    /// <summary>STEP 3 — Optimized single-query photo retrieval for new (IsLatest=true) and old (IsLatest=false).</summary>
    private async Task<List<ReassessmentPhotoDto>> GetPhotosAsync(
        int propertyId,
        List<int> oldPropertyIds,
        CancellationToken cancellationToken)
    {
        // Get photo type IDs (combine both queries into one)
        var photoTypesByCode = await _propertyPhotoTypeRepository.GetQueryable()
            .Where(pt => pt.PhotoTypeCode == PlanPhotoCode || pt.PhotoTypeCode == PropertyPhotoCode)
            .GroupBy(pt => pt.PhotoTypeCode)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Id).ToList(), cancellationToken);

        if (photoTypesByCode.Count == 0)
            return [];

        var planTypeIds = photoTypesByCode.ContainsKey(PlanPhotoCode) ? photoTypesByCode[PlanPhotoCode] : [];
        var propertyTypeIds = photoTypesByCode.ContainsKey(PropertyPhotoCode) ? photoTypesByCode[PropertyPhotoCode] : [];

        var photoTypeIdSet = new HashSet<int>(planTypeIds.Concat(propertyTypeIds));
        if (photoTypeIdSet.Count == 0)
            return [];

        // Single consolidated query for all photos (NEW + OLD)
        var allPhotoBindings = await (
            from pp in _propertyPhotoRepository.GetQueryable()
            where pp.PropertyId == propertyId && pp.IsActive && photoTypeIdSet.Contains(pp.PhotoTypeId) && pp.DocumentBindingId != null
            select new
            {
                BindingId = pp.DocumentBindingId!.Value,
                pp.IsLatest,
                pp.PhotoTypeId,
                SortDate = pp.UpdatedDate ?? pp.CreatedDate,
                IsOld = false,
                OldPropertyId = (int?)null
            }
        ).Union(
            from ppo in _propertyPhotoOldRepository.GetQueryable()
             where oldPropertyIds.Contains(ppo.PropertyMastOldId) && ppo.IsActive && !ppo.MarkedForDeletion && !ppo.IsLatest && photoTypeIdSet.Contains(ppo.PhotoTypeId) && ppo.DocumentBindingId != null
             select new
             {
                 BindingId = ppo.DocumentBindingId!.Value,
                 ppo.IsLatest,
                 ppo.PhotoTypeId,
                 SortDate = ppo.UpdatedDate ?? ppo.CreatedDate,
                 IsOld = true,
                 OldPropertyId = (int?)ppo.PropertyMastOldId
             }
        ).ToListAsync(cancellationToken);

        // Get document GUIDs for all bindings in one query
        var bindingIds = allPhotoBindings.Select(p => p.BindingId).Distinct().ToList();
        var documentGuids = await (
            from b in _documentBindingRepository.GetQueryable()
            join d in _documentRepository.GetQueryable() on b.DocumentId equals d.Id
            where bindingIds.Contains(b.Id) && d.IsActive
            select new { b.Id, d.DocumentGuid }
        ).ToDictionaryAsync(x => x.Id, x => x.DocumentGuid, cancellationToken);

        var photos = new List<ReassessmentPhotoDto>();

        void AddPhotoIfFound(object? photo, List<int> typeIds, string type)
        {
            if (photo == null)
                return;

            var binding = (dynamic)photo;
            if (documentGuids.TryGetValue(binding.BindingId, out Guid guid))
            {
                photos.Add(new ReassessmentPhotoDto { DocumentGuid = guid, Type = type });
            }
        }

        // Process NEW photos (latest per type)
        var newPlanPhoto = allPhotoBindings.Where(p => !p.IsOld && p.IsLatest && planTypeIds.Contains(p.PhotoTypeId)).OrderByDescending(p => p.SortDate).FirstOrDefault();
        AddPhotoIfFound(newPlanPhoto, planTypeIds, "NEW_PLAN_PHOTO");

        var newPropertyPhoto = allPhotoBindings.Where(p => !p.IsOld && p.IsLatest && propertyTypeIds.Contains(p.PhotoTypeId)).OrderByDescending(p => p.SortDate).FirstOrDefault();
        AddPhotoIfFound(newPropertyPhoto, propertyTypeIds, "NEW_PROPERTY_PHOTO");

        // Process OLD photos (latest per type across all old properties)
        if (oldPropertyIds.Count > 0)
        {
            var oldPlanPhoto = allPhotoBindings.FirstOrDefault(p => p.IsOld && !p.IsLatest && planTypeIds.Contains(p.PhotoTypeId));
            AddPhotoIfFound(oldPlanPhoto, planTypeIds, "OLD_PLAN_PHOTO");

            var oldPropertyPhoto = allPhotoBindings.FirstOrDefault(p => p.IsOld && !p.IsLatest && propertyTypeIds.Contains(p.PhotoTypeId));
            AddPhotoIfFound(oldPropertyPhoto, propertyTypeIds, "OLD_PROPERTY_PHOTO");
        }

        return photos;
    }

    /// <summary>STEP 4 (new) — PropertyDetails + master codes + latest-year renter + RV calculation figures + certificates (separate OC/CC fields).</summary>
    private async Task<List<ReassessmentFloorDto>> GetNewFloorDetailsAsync(
        int propertyId,
        Dictionary<(int PropertyId, int? DetailId), (string? CertNo, DateTime? IssueDate)> ocCerts,
        Dictionary<(int PropertyId, int? DetailId), (string? CertNo, DateTime? IssueDate)> ccCerts,
        CancellationToken cancellationToken)
    {
        var detailRows = await (
            from pd in _propertyDetailsRepository.GetQueryable()
            where pd.PropertyId == propertyId && pd.IsActive && !pd.MarkedForDeletion
            join f in _floorRepository.GetQueryable() on pd.FloorId equals f.Id into fj
            from f in fj.DefaultIfEmpty()
            join ct in _constructionTypeRepository.GetQueryable() on pd.ConstructionTypeId equals ct.Id into ctj
            from ct in ctj.DefaultIfEmpty()
            join tu in _typeOfUseRepository.GetQueryable() on pd.TypeOfUseId equals tu.Id into tuj
            from tu in tuj.DefaultIfEmpty()
            select new
            {
                pd.Id,
                FloorCode = f != null ? f.FloorCode : null,
                ConstructionCode = ct != null ? ct.ConstructionCode : null,
                Description = tu != null ? tu.Description : null,
                pd.ConstructionYear,
                pd.AssessmentYear,
                pd.CarpetAreaSqMeter,
                pd.CarpetAreaSqFeet,
                pd.BuiltupAreaSqMeter,
                pd.BuiltupAreaSqFeet,
                IsRenter = pd.IsRenter ?? false
            })
            .ToListAsync(cancellationToken);

        if (detailRows.Count == 0)
            return [];

        var detailIds = detailRows.Select(d => d.Id).ToList();

        // Renters: latest active finance year only, combined with YearMaster.Year lookup in single query.
        var renterByDetailId = await (
            from r in _renterRepository.GetQueryable()
            join y in _yearMasterRepository.GetQueryable() on r.FinancialYear equals y.Year.ToString()
            where r.IsActive && !r.MarkedForDeletion && y.IsActive && detailIds.Contains(r.PropertyDetailsId)
            orderby y.Year descending, r.PropertyDetailsId
            select new { r.PropertyDetailsId, r.RenterName, r.TaxLiability, r.RentMonthly, r.FinalYearlyRent, r.FinancialYear }
        )
        .GroupBy(r => r.PropertyDetailsId)
        .Select(g => new
        {
            DetailId = g.Key,
            Data = new { g.First().RenterName, g.First().TaxLiability, g.First().RentMonthly, g.First().FinalYearlyRent, g.First().FinancialYear }
        })
        .ToDictionaryAsync(
            g => g.DetailId,
            g => (g.Data.RenterName, g.Data.TaxLiability, g.Data.RentMonthly, g.Data.FinalYearlyRent, g.Data.FinancialYear),
            cancellationToken);

        // RV calculation figures are detail-level but stored once per tax row — collapse to one per detail
        // (the SP's SELECT DISTINCT) to avoid multiplying floor rows.
        var rvRows = await _rvResultsRepository.GetQueryable()
            .Where(rv => rv.PropertyId == propertyId
                         && detailIds.Contains(rv.PropertyDetailsId)
                         && rv.IsActive
                         && !rv.MarkedForDeletion)
            .Select(rv => new
            {
                rv.PropertyDetailsId,
                rv.RateableValue,
                rv.AnnualRentalValue,
                rv.Depreciation,
                rv.Maintenance,
                rv.MonthlyRate,
                rv.YearlyRate,
                rv.YearlyRent
            })
            .ToListAsync(cancellationToken);

        var rvByDetailId = rvRows
            .GroupBy(rv => rv.PropertyDetailsId)
            .ToDictionary(g => g.Key, g => g.First());

        return detailRows.Select(d =>
        {
            var dto = new ReassessmentFloorDto
            {
                Type = "NEW",
                FloorCode = d.FloorCode,
                ConstructionCode = d.ConstructionCode,
                Description = d.Description,
                ConstructionYear = d.ConstructionYear,
                AssessmentYear = d.AssessmentYear,
                CarpetAreaSqMeter = d.CarpetAreaSqMeter,
                CarpetAreaSqFeet = d.CarpetAreaSqFeet,
                BuiltupAreaSqMeter = d.BuiltupAreaSqMeter,
                BuiltupAreaSqFeet = d.BuiltupAreaSqFeet,
                IsRenter = d.IsRenter
            };

            if (d.IsRenter && renterByDetailId.TryGetValue(d.Id, out var renter))
            {
                dto.RenterName = renter.RenterName;
                dto.TaxLiability = renter.TaxLiability;
                dto.RentMonthly = renter.RentMonthly;
                dto.FinalYearlyRent = renter.FinalYearlyRent;
                dto.FinancialYear = renter.FinancialYear;
            }

            if (rvByDetailId.TryGetValue(d.Id, out var rv))
            {
                dto.RateableValue = rv.RateableValue;
                dto.AnnualRentalValue = rv.AnnualRentalValue;
                dto.Depreciation = rv.Depreciation;
                dto.Maintenance = rv.Maintenance;
                dto.MonthlyRate = rv.MonthlyRate;
                dto.YearlyRate = rv.YearlyRate;
                dto.YearlyRent = rv.YearlyRent;
            }

            if (ocCerts.TryGetValue((propertyId, d.Id), out var ocCert))
            {
                dto.OCCertificateNo = ocCert.CertNo;
                dto.OCCertificateIssueDate = ocCert.IssueDate;
            }

            if (ccCerts.TryGetValue((propertyId, d.Id), out var ccCert))
            {
                dto.CCCertificateNo = ccCert.CertNo;
                dto.CCCertificateIssueDate = ccCert.IssueDate;
            }

            return dto;
        }).ToList();
    }

    /// <summary>
    /// STEP 4 (old) — PropertyDetailsOld + master codes from ALL mapped old properties.
    /// RateableValue/AnnualRentalValue sourced from each row's PropertyMastOld (OldRV/OldALV).
    /// Other calculated fields (Depreciation, Maintenance, etc.) always 0 for old records.
    /// For MERGE scenarios: returns rows from all mapped old properties, each tagged with PropertyIdOld.
    /// Master-table lookups (Floor, ConstructionType, TypeOfUse) performed via pre-fetched dictionaries
    /// to avoid SQL-level join multiplication risk.
    /// </summary>
    private async Task<List<ReassessmentFloorDto>> GetOldFloorDetailsAsync(
        List<int> oldPropertyIds,
        CancellationToken cancellationToken)
    {
        // Pre-fetch all PropertyMastOld rows to avoid repeated lookups
        var oldMastDict = await _propertyMastOldRepository.GetQueryable()
            .Where(pm => oldPropertyIds.Contains(pm.Id))
            .ToDictionaryAsync(pm => pm.Id, pm => new { pm.OldRV, pm.OldALV }, cancellationToken);

        // Fetch PropertyDetailsOld rows with their FK values, filtering by IsActive and MarkedForDeletion
        var oldDetailRows = await _propertyDetailsOldRepository.GetQueryable()
            .Where(pd => oldPropertyIds.Contains(pd.PropertyMastOldId) && pd.IsActive && !pd.MarkedForDeletion)
            .Select(pd => new
            {
                pd.PropertyMastOldId,
                pd.OldFloorId,
                pd.OldConstructionTypeId,
                pd.OldTypeOfUseId,
                pd.OldConstructionYear,
                pd.OldAssessmentYear,
                pd.OldCarpetAreaSqMeter,
                pd.OldCarpetAreaSqFeet,
                pd.OldBuiltupAreaSqMeter,
                pd.OldBuiltupAreaSqFeet
            })
            .ToListAsync(cancellationToken);

        if (oldDetailRows.Count == 0)
            return [];

        // Pre-fetch all needed master records by ID
        var floorIds = oldDetailRows.Where(d => d.OldFloorId.HasValue).Select(d => d.OldFloorId!.Value).Distinct().ToList();
        var constructionTypeIds = oldDetailRows.Where(d => d.OldConstructionTypeId.HasValue).Select(d => d.OldConstructionTypeId!.Value).Distinct().ToList();
        var typeOfUseIds = oldDetailRows.Where(d => d.OldTypeOfUseId.HasValue).Select(d => d.OldTypeOfUseId!.Value).Distinct().ToList();

        var floorDict = floorIds.Count > 0
            ? await _floorRepository.GetQueryable()
                .Where(f => floorIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.FloorCode, cancellationToken)
            : new Dictionary<int, string>();

        var constructionTypeDict = constructionTypeIds.Count > 0
            ? await _constructionTypeRepository.GetQueryable()
                .Where(ct => constructionTypeIds.Contains(ct.Id))
                .ToDictionaryAsync(ct => ct.Id, ct => ct.ConstructionCode, cancellationToken)
            : new Dictionary<int, string>();

        var typeOfUseDict = typeOfUseIds.Count > 0
            ? await _typeOfUseRepository.GetQueryable()
                .Where(tu => typeOfUseIds.Contains(tu.Id))
                .ToDictionaryAsync(tu => tu.Id, tu => tu.Description, cancellationToken)
            : new Dictionary<int, string>();

        // Build DTOs in-memory with dictionary lookups (no SQL-level joins, no row multiplication possible)
        return oldDetailRows.Select(pd => new ReassessmentFloorDto
        {
            Type = "OLD",
            PropertyIdOld = pd.PropertyMastOldId,
            FloorCode = pd.OldFloorId.HasValue && floorDict.TryGetValue(pd.OldFloorId.Value, out var fc) ? fc : null,
            ConstructionCode = pd.OldConstructionTypeId.HasValue && constructionTypeDict.TryGetValue(pd.OldConstructionTypeId.Value, out var cc) ? cc : null,
            Description = pd.OldTypeOfUseId.HasValue && typeOfUseDict.TryGetValue(pd.OldTypeOfUseId.Value, out var desc) ? desc : null,
            ConstructionYear = pd.OldConstructionYear,
            AssessmentYear = pd.OldAssessmentYear,
            CarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
            CarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
            BuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
            BuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
            RateableValue = oldMastDict.TryGetValue(pd.PropertyMastOldId, out var m1) ? (decimal?)m1.OldRV : null,
            AnnualRentalValue = oldMastDict.TryGetValue(pd.PropertyMastOldId, out var m2) ? m2.OldALV : null,
            Depreciation = 0m,
            Maintenance = 0m,
            MonthlyRate = 0d,
            YearlyRate = 0d,
            YearlyRent = 0d
            // Certificate fields left as null for OLD rows (no certificate support for old properties)
        }).ToList();
    }

    /// <summary>
    /// Marks each floor row Unchanged/Added/Removed by comparing FloorCode+ConstructionCode+Description
    /// (type-of-use) between the two lists. Rows sharing a key on both sides are Unchanged even if other
    /// fields (areas, rates, etc.) differ. When several rows share the same key on one side, matching is by
    /// key existence, not 1:1 pairing — all of them get the same status.
    /// </summary>
    private static void ApplyFloorChangeStatus(List<ReassessmentFloorDto> newRows, List<ReassessmentFloorDto> oldRows)
    {
        static string Key(ReassessmentFloorDto d) =>
            $"{d.FloorCode ?? string.Empty}|{d.ConstructionCode ?? string.Empty}|{d.Description ?? string.Empty}";

        var newKeys = newRows.Select(Key).ToHashSet();
        var oldKeys = oldRows.Select(Key).ToHashSet();

        foreach (var row in newRows)
            row.ChangeStatus = oldKeys.Contains(Key(row)) ? "Unchanged" : "Added";

        foreach (var row in oldRows)
            row.ChangeStatus = newKeys.Contains(Key(row)) ? "Unchanged" : "Removed";
    }

    /// <summary>
    /// STEP 5 — old (TransMastOld) vs new (TransMast) amount per active tax head, ordered by DisplayOrder.
    /// Old amounts are aggregated across ALL mapped old properties (important for MERGE scenarios).
    /// Example: Old100 Tax=100 + Old101 Tax=200 + Old102 Tax=300 → OldAmount=600.
    /// </summary>
    private async Task<List<ReassessmentTaxHeadDto>> GetTaxSummaryAsync(
        int propertyId,
        List<int> oldPropertyIds,
        CancellationToken cancellationToken)
    {
        var taxes = await _taxMasterRepository.GetQueryable()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.TaxName, t.DisplayOrder })
            .ToListAsync(cancellationToken);

        if (taxes.Count == 0)
            return [];

        var newByTaxId = await _transMastRepository.GetQueryable()
            .Where(t => t.PropertyId == propertyId && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.TaxId)
            .Select(g => new { TaxId = g.Key, Amount = g.Sum(x => x.TaxAmount) })
            .ToDictionaryAsync(x => x.TaxId, x => x.Amount, cancellationToken);

        var oldByTaxId = new Dictionary<int, decimal>();
        if (oldPropertyIds.Count > 0)
        {
            oldByTaxId = await _transMastOldRepository.GetQueryable()
                .Where(t => oldPropertyIds.Contains(t.PropertyMastOldId) && t.IsActive && !t.MarkedForDeletion)
                .GroupBy(t => t.TaxId)
                .Select(g => new { TaxId = g.Key, Amount = g.Sum(x => x.TaxAmount) })
                .ToDictionaryAsync(x => x.TaxId, x => x.Amount, cancellationToken);
        }

        return taxes.Select(t => new ReassessmentTaxHeadDto
        {
            TaxId = t.Id,
            TaxName = t.TaxName,
            DisplayOrder = t.DisplayOrder,
            OldAmount = oldByTaxId.GetValueOrDefault(t.Id),
            NewAmount = newByTaxId.GetValueOrDefault(t.Id)
        }).ToList();
    }
}
