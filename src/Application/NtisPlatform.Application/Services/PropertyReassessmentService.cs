using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyReassessment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Re-implements the legacy "Property Re-Assessment" SQL script (single-property variant) in application
/// code. Read-only: resolves the property from Ward + PropertyNo (+ optional PartitionNo), then assembles
/// old/new photos (STEP 2), old/new floor details (STEP 3) and the old-vs-new tax-head summary (STEP 4).
///
/// Per the repo convention (see <see cref="DataEntrySameAsService"/> and PropertyOldDetailsRepository),
/// everything is EF Core LINQ over <see cref="IRepository{T,TKey}"/> — no raw SQL. The dynamic PIVOT of
/// STEP 4 is replaced by an in-memory tax-head projection.
/// </summary>
public class PropertyReassessmentService : IPropertyReassessmentService
{
    private const string PlanPhotoCode = "PLAN_PHOTO";
    private const string PropertyPhotoCode = "PROPERTY_PHOTO";

    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyDetailsOldEntity, int> _propertyDetailsOldRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyPhotoEntity, int> _propertyPhotoRepository;
    private readonly IRepository<PropertyPhotoTypeEntity, int> _propertyPhotoTypeRepository;
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<DocumentBindingEntity, int> _documentBindingRepository;
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<RenterMastEntity, int> _renterRepository;
    private readonly IRepository<PropertyTaxCalculationRVResultsEntity, int> _rvResultsRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<TransMastOldEntity, int> _transMastOldRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;

    public PropertyReassessmentService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyDetailsOldEntity, int> propertyDetailsOldRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<PropertyPhotoEntity, int> propertyPhotoRepository,
        IRepository<PropertyPhotoTypeEntity, int> propertyPhotoTypeRepository,
        IRepository<DocumentEntity, int> documentRepository,
        IRepository<DocumentBindingEntity, int> documentBindingRepository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<ConstructionTypeEntity, int> constructionTypeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<RenterMastEntity, int> renterRepository,
        IRepository<PropertyTaxCalculationRVResultsEntity, int> rvResultsRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<TransMastOldEntity, int> transMastOldRepository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository)
    {
        _propertyRepository = propertyRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyDetailsOldRepository = propertyDetailsOldRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyPhotoRepository = propertyPhotoRepository;
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
    }

    public async Task<PropertyReassessmentDto> GetReassessmentAsync(
        PropertyReassessmentQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        // ── STEP 1: resolve the single property (new id + old id) ─────────────
        var propertyNo = query.PropertyNo;
        var partitionNo = query.PartitionNo?.Trim();
        var hasPartition = !string.IsNullOrEmpty(partitionNo);

        // Tightened vs the SP's loose "ISNULL(PartitionNo,'')='' OR PartitionNo=@x": a supplied partition
        // must match exactly; an omitted partition matches only building-level (empty/null) rows. This,
        // combined with the uniqueness check below, guarantees the screen shows exactly one property.
        var matches = await _propertyRepository.GetQueryable()
            .Where(p => p.WardId == query.WardId
                        && p.PropertyNo == propertyNo
                        && p.IsActive
                        && !p.MarkedForDeletion
                        && (hasPartition
                                ? p.PartitionNo == partitionNo
                                : (p.PartitionNo == null || p.PartitionNo == "")))
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
            throw new ArgumentException("No property found for the supplied Ward, Property No and Partition No.");

        if (matches.Count > 1)
            throw new ArgumentException(
                "More than one property matches the supplied Ward and Property No. Please specify a Partition No.");

        var propertyId = matches[0].Id;
        var propertyOldId = matches[0].PropertyMastOldId;

        var result = new PropertyReassessmentDto
        {
            PropertyId = propertyId,
            PropertyOldId = propertyOldId
        };

        // ── STEP 2: photos (old = superseded/IsLatest 0, new = current/IsLatest 1) ──
        result.Photos = await GetPhotosAsync(propertyId, cancellationToken);

        // ── STEP 3: floor details (new from PropertyDetails, old from PropertyDetailsOld) ──
        result.NewFloorDetails = await GetNewFloorDetailsAsync(propertyId, cancellationToken);
        result.OldFloorDetails = propertyOldId.HasValue
            ? await GetOldFloorDetailsAsync(propertyOldId.Value, cancellationToken)
            : [];

        ApplyFloorChangeStatus(result.NewFloorDetails, result.OldFloorDetails);

        // ── STEP 4: tax-head summary (old vs new) ─────────────────────────────
        result.TaxSummary = await GetTaxSummaryAsync(propertyId, propertyOldId, cancellationToken);

        return result;
    }

    /// <summary>STEP 2 — the latest plan/property document for old (IsLatest=false) and new (IsLatest=true).</summary>
    private async Task<List<ReassessmentPhotoDto>> GetPhotosAsync(int propertyId, CancellationToken cancellationToken)
    {
        var planTypeIds = await _propertyPhotoTypeRepository.GetQueryable()
            .Where(pt => pt.PhotoTypeCode == PlanPhotoCode)
            .Select(pt => pt.Id)
            .ToListAsync(cancellationToken);

        var propertyTypeIds = await _propertyPhotoTypeRepository.GetQueryable()
            .Where(pt => pt.PhotoTypeCode == PropertyPhotoCode)
            .Select(pt => pt.Id)
            .ToListAsync(cancellationToken);

        var photos = new List<ReassessmentPhotoDto>();

        await AddLatestPhotoAsync(photos, propertyId, planTypeIds, isLatest: false, "OLD_PLAN_PHOTO", cancellationToken);
        await AddLatestPhotoAsync(photos, propertyId, propertyTypeIds, isLatest: false, "OLD_PROPERTY_PHOTO", cancellationToken);
        await AddLatestPhotoAsync(photos, propertyId, planTypeIds, isLatest: true, "NEW_PLAN_PHOTO", cancellationToken);
        await AddLatestPhotoAsync(photos, propertyId, propertyTypeIds, isLatest: true, "NEW_PROPERTY_PHOTO", cancellationToken);

        return photos;
    }

    /// <summary>
    /// Mirrors the SP's "TOP 1 photo ordered by ISNULL(UpdatedDate,CreatedDate) DESC, then look up the
    /// active document" — two steps so an inactive document yields no photo (rather than picking the next one).
    /// </summary>
    private async Task AddLatestPhotoAsync(
        List<ReassessmentPhotoDto> photos,
        int propertyId,
        List<int> photoTypeIds,
        bool isLatest,
        string type,
        CancellationToken cancellationToken)
    {
        if (photoTypeIds.Count == 0)
            return;

        var bindingId = await _propertyPhotoRepository.GetQueryable()
            .Where(p => p.PropertyId == propertyId
                        && p.IsActive
                        && p.IsLatest == isLatest
                        && photoTypeIds.Contains(p.PhotoTypeId)
                        && p.DocumentBindingId != null)
            .OrderByDescending(p => p.UpdatedDate ?? p.CreatedDate)
            .Select(p => p.DocumentBindingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (bindingId == null)
            return;

        var documentGuid = await (
            from b in _documentBindingRepository.GetQueryable()
            join d in _documentRepository.GetQueryable() on b.DocumentId equals d.Id
            where b.Id == bindingId.Value && d.IsActive
            select (Guid?)d.DocumentGuid
        ).FirstOrDefaultAsync(cancellationToken);

        if (documentGuid.HasValue)
            photos.Add(new ReassessmentPhotoDto { DocumentGuid = documentGuid.Value, Type = type });
    }

    /// <summary>STEP 3 (new) — PropertyDetails + master codes + latest-year renter + RV calculation figures.</summary>
    private async Task<List<ReassessmentFloorDto>> GetNewFloorDetailsAsync(int propertyId, CancellationToken cancellationToken)
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

        // Renters: latest active finance year only (SP: RM.FinancialYear = MAX(active YearMaster.Year)).
        var maxActiveYear = await _yearMasterRepository.GetQueryable()
            .Where(y => y.IsActive)
            .Select(y => (int?)y.Year)
            .MaxAsync(cancellationToken);

        var renterByDetailId = new Dictionary<int, (string? RenterName, string? TaxLiability, double? RentMonthly, double? FinalYearlyRent, string? FinancialYear)>();
        if (maxActiveYear.HasValue)
        {
            var maxYearText = maxActiveYear.Value.ToString();
            var renterRows = await _renterRepository.GetQueryable()
                .Where(r => r.IsActive && !r.MarkedForDeletion && r.FinancialYear == maxYearText && detailIds.Contains(r.PropertyDetailsId))
                .Select(r => new
                {
                    r.PropertyDetailsId,
                    r.RenterName,
                    r.TaxLiability,
                    r.RentMonthly,
                    r.FinalYearlyRent,
                    r.FinancialYear
                })
                .ToListAsync(cancellationToken);

            renterByDetailId = renterRows
                .GroupBy(r => r.PropertyDetailsId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var r = g.First();
                        return (r.RenterName, r.TaxLiability, r.RentMonthly, r.FinalYearlyRent, r.FinancialYear);
                    });
        }

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

            return dto;
        }).ToList();
    }

    /// <summary>
    /// STEP 3 (old) — PropertyDetailsOld + master codes, with RateableValue/AnnualRentalValue sourced
    /// from PropertyMastOld (OldRV/OldALV); Depreciation/Maintenance/MonthlyRate/YearlyRate/YearlyRent
    /// aren't tracked for old records and stay 0. Every row shares the same PropertyMastOldId (the
    /// method's input), so PropertyMastOld is looked up once rather than joined per row.
    /// </summary>
    private async Task<List<ReassessmentFloorDto>> GetOldFloorDetailsAsync(int propertyOldId, CancellationToken cancellationToken)
    {
        var oldMast = await _propertyMastOldRepository.GetQueryable()
            .Where(pm => pm.Id == propertyOldId)
            .Select(pm => new { pm.OldRV, pm.OldALV })
            .FirstOrDefaultAsync(cancellationToken);

        return await (
            from pd in _propertyDetailsOldRepository.GetQueryable()
            where pd.PropertyMastOldId == propertyOldId && pd.IsActive
            join f in _floorRepository.GetQueryable() on pd.OldFloorId equals f.Id into fj
            from f in fj.DefaultIfEmpty()
            join ct in _constructionTypeRepository.GetQueryable() on pd.OldConstructionTypeId equals ct.Id into ctj
            from ct in ctj.DefaultIfEmpty()
            join tu in _typeOfUseRepository.GetQueryable() on pd.OldTypeOfUseId equals tu.Id into tuj
            from tu in tuj.DefaultIfEmpty()
            select new ReassessmentFloorDto
            {
                Type = "OLD",
                FloorCode = f != null ? f.FloorCode : null,
                ConstructionCode = ct != null ? ct.ConstructionCode : null,
                Description = tu != null ? tu.Description : null,
                ConstructionYear = pd.OldConstructionYear,
                AssessmentYear = pd.OldAssessmentYear,
                CarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                CarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                BuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                BuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                RateableValue = (decimal?)(oldMast != null ? oldMast.OldRV : null),
                AnnualRentalValue = oldMast != null ? oldMast.OldALV : null,
                Depreciation = 0m,
                Maintenance = 0m,
                MonthlyRate = 0d,
                YearlyRate = 0d,
                YearlyRent = 0d
            })
            .ToListAsync(cancellationToken);
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
    /// STEP 4 — old (TransMastOld) vs new (TransMast) amount per active tax head, ordered by DisplayOrder.
    /// Replaces the dynamic PIVOT: amounts are summed per tax head into a single old/new figure (the screen
    /// shows one old row and one new row), which for the common single-finance-year case equals the SP output.
    /// </summary>
    private async Task<List<ReassessmentTaxHeadDto>> GetTaxSummaryAsync(
        int propertyId,
        int? propertyOldId,
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
        if (propertyOldId.HasValue)
        {
            oldByTaxId = await _transMastOldRepository.GetQueryable()
                .Where(t => t.PropertyMastOldId == propertyOldId.Value && t.IsActive && !t.MarkedForDeletion)
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
