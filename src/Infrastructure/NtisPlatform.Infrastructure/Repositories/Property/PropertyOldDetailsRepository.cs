using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Data-access implementation for the Property "Old Details" tab (queries, staged inserts and the
/// old-taxes write transaction). Split across partial files per sub-section: this file covers old
/// property details; <c>PropertyOldDetailsRepository.OldTaxesAndFloor.cs</c> covers old taxes and old floor.
/// </summary>
public partial class PropertyOldDetailsRepository : PropertyRepositoryBase, IPropertyOldDetailsRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public PropertyOldDetailsRepository(ApplicationDbContext context, IUnitOfWork unitOfWork)
        : base(context)
    {
        _unitOfWork = unitOfWork;
    }

    // ---- Old Property Details sub-section ----

    public async Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get property if it exists
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Resolve all mapped old property IDs (from PropertyMapDetail and/or direct PropertyMastOldId)
        var mappedOldPropertyIds = await _context.PropertyMapDetails
            .AsNoTracking()
            .Where(pmd => pmd.PropertyIdNew == propertyId && pmd.IsActive && pmd.IsCurrent && pmd.Status == "ACTIVE")
            .Select(pmd => pmd.PropertyIdOld)
            .Where(id => id != null)
            .Distinct()
            .ToListAsync(cancellationToken);

        var oldPropertyIds = mappedOldPropertyIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (property.PropertyMastOldId.HasValue && !oldPropertyIds.Contains(property.PropertyMastOldId.Value))
        {
            oldPropertyIds.Add(property.PropertyMastOldId.Value);
        }

        if (!oldPropertyIds.Any())
            return new PropertyOldDetailsDto { PropertyId = propertyId };

        // Step 3: Fetch PropertyMastOld data
        var pmoList = await _context.PropertyMastOld
            .AsNoTracking()
            .Where(x => oldPropertyIds.Contains(x.Id) && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Step 4: Fetch PropertyDetailsOld data
        var pdoList = await _context.PropertyDetailsOld
            .AsNoTracking()
            .Where(x => oldPropertyIds.Contains(x.PropertyMastOldId) && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Step 5: Load TransMastOld and TaxMaster records to calculate OldTotalTax and OldGeneralTax accurately
        var transMastOldRecords = await _context.TransMastOld
            .AsNoTracking()
            .Where(t => oldPropertyIds.Contains(t.PropertyMastOldId) && t.IsActive && !t.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var oldTaxes = await _context.TaxMaster
            .AsNoTracking()
            .Where(t => t.IsActive && t.OldTaxStatus)
            .Select(t => new { t.Id, t.TaxName, t.TaxNameAlias })
            .ToListAsync(cancellationToken);

        var interestTaxId = oldTaxes.FirstOrDefault(t =>
            t.TaxName.Equals("Interest", StringComparison.OrdinalIgnoreCase) ||
            (t.TaxNameAlias != null && t.TaxNameAlias.Equals("Interest", StringComparison.OrdinalIgnoreCase)))?.Id;

        var generalTaxId = oldTaxes.FirstOrDefault(t =>
            t.TaxName.Equals("General Tax", StringComparison.OrdinalIgnoreCase) ||
            t.TaxName.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
            (t.TaxNameAlias != null && (t.TaxNameAlias.Equals("General Tax", StringComparison.OrdinalIgnoreCase) ||
                                        t.TaxNameAlias.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase))))?.Id;

        // Step 6: Perform sums for OldRV, OldALV, OldTotalTax, OldGeneralTax, and OldConstructionArea
        double totalOldRV = 0;
        double totalOldALV = 0;
        double totalOldTotalTax = 0;
        double totalOldConstructionArea = 0;
        var generalTaxValues = new List<double>();

        foreach (var pmo in pmoList)
        {
            totalOldRV += pmo.OldRV ?? 0;
            totalOldALV += pmo.OldALV ?? 0;
            totalOldConstructionArea += pmo.OldConstructionArea ?? 0;

            var hasTrans = transMastOldRecords.Any(t => t.PropertyMastOldId == pmo.Id);
            if (hasTrans)
            {
                var sumTax = transMastOldRecords
                    .Where(t => t.PropertyMastOldId == pmo.Id && (!interestTaxId.HasValue || t.TaxId != interestTaxId.Value))
                    .Sum(t => (double?)t.TaxAmount) ?? 0;
                totalOldTotalTax += sumTax;

                if (generalTaxId.HasValue)
                {
                    var genTax = transMastOldRecords
                        .Where(t => t.PropertyMastOldId == pmo.Id && t.TaxId == generalTaxId.Value)
                        .Sum(t => (double?)t.TaxAmount) ?? 0;
                    generalTaxValues.Add(genTax);
                }
                else if (pmo.OldGeneralTax.HasValue)
                {
                    generalTaxValues.Add(pmo.OldGeneralTax.Value);
                }
            }
            else
            {
                totalOldTotalTax += pmo.OldTotalTax ?? 0;
                if (pmo.OldGeneralTax.HasValue)
                {
                    generalTaxValues.Add(pmo.OldGeneralTax.Value);
                }
            }
        }

        double totalOldGeneralTax = generalTaxValues.Sum();

        // Sum carpet area columns from PDO
        double totalOldCarpetAreaSqFeet = pdoList.Sum(x => x.OldCarpetAreaSqFeet ?? 0);
        double totalOldCarpetAreaSqMeter = pdoList.Sum(x => x.OldCarpetAreaSqMeter ?? 0);

        // Step 7: Process distinct comma-separated values for PMO properties
        var oldWardNoList = pmoList.Select(x => x.OldWardNo?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldPropertyNoList = pmoList.Select(x => x.OldPropertyNo?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldPartitionNoList = pmoList.Select(x => x.OldPartitionNo?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldEgovNoList = pmoList.Select(x => x.OldEgovNo?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldPlotAreaList = pmoList.Where(x => x.OldPlotArea.HasValue).Select(x => Math.Round(x.OldPlotArea!.Value, 2).ToString()).Distinct().ToList();
        var oldPlotNoList = pmoList.Select(x => x.OldPlotNo?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldZoneNoList = pmoList.Select(x => x.OldZoneNo?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldCSNList = pmoList.Select(x => x.OldCSN?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

        // Step 8: Process distinct comma-separated values for PDO properties
        var oldConstructionYearList = pdoList.Select(x => x.OldConstructionYear?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var oldConstructionTypeIdList = pdoList.Select(x => x.OldConstructionTypeId.ToString()).Distinct().ToList();
        var oldTypeOfUseIdList = pdoList.Select(x => x.OldTypeOfUseId.ToString()).Distinct().ToList();

        // Build and return DTO
        return new PropertyOldDetailsDto
        {
            PropertyId = propertyId,
            OldWardNo = oldWardNoList.Any() ? string.Join(", ", oldWardNoList) : null,
            OldPropertyNo = oldPropertyNoList.Any() ? string.Join(", ", oldPropertyNoList) : null,
            OldPartitionNo = oldPartitionNoList.Any() ? string.Join(", ", oldPartitionNoList) : null,
            OldEgovNo = oldEgovNoList.Any() ? string.Join(", ", oldEgovNoList) : null,
            OldPlotArea = oldPlotAreaList.Any() ? string.Join(", ", oldPlotAreaList) : null,
            OldPlotNo = oldPlotNoList.Any() ? string.Join(", ", oldPlotNoList) : null,
            OldRV = Math.Round(totalOldRV, 2),
            OldALV = Math.Round(totalOldALV, 2),
            OldTotalTax = Math.Round(totalOldTotalTax, 2),
            OldZoneNo = oldZoneNoList.Any() ? string.Join(", ", oldZoneNoList) : null,
            OldGeneralTax = Math.Round(totalOldGeneralTax, 2),
            OldCSN = oldCSNList.Any() ? string.Join(", ", oldCSNList) : null,
            OldConstructionArea = Math.Round(totalOldConstructionArea, 2),
            OldConstructionYear = oldConstructionYearList.Any() ? string.Join(", ", oldConstructionYearList) : null,
            OldCarpetAreaSqFeet = Math.Round(totalOldCarpetAreaSqFeet, 2),
            OldCarpetAreaSqMeter = Math.Round(totalOldCarpetAreaSqMeter, 2),
            OldConstructionTypeId = oldConstructionTypeIdList.Any() ? string.Join(", ", oldConstructionTypeIdList) : null,
            OldTypeOfUseId = oldTypeOfUseIdList.Any() ? string.Join(", ", oldTypeOfUseIdList) : null
        };
    }

    public async Task<PropertyTabHeaderInfoDto?> GetTabHeaderInfoAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var result = await (from p in _context.PropertyMast.AsNoTracking()
                            where p.Id == propertyId && p.IsActive && !p.MarkedForDeletion

                            join pas in _context.PropertyAssessmentStatuses.AsNoTracking() on p.PropertyAssessmentStatusId equals pas.Id into pasJoin
                            from pas in pasJoin.Where(x => x.IsActive).DefaultIfEmpty()

                            join pmo in _context.PropertyMastOld.AsNoTracking() on p.PropertyMastOldId equals pmo.Id into pmoJoin
                            from pmo in pmoJoin.Where(x => x.IsActive && !x.MarkedForDeletion).DefaultIfEmpty()

                            join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                            join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into typeJoin
                            from pt in typeJoin.Where(x => x.IsActive).DefaultIfEmpty()

                            select new PropertyTabHeaderInfoDto
                            {
                                PropertyId = p.Id,
                                StatusName = pas != null ? pas.StatusName : null,
                                OldWardNo = pmo != null ? pmo.OldWardNo : null,
                                OldPropertyNo = pmo != null ? pmo.OldPropertyNo : null,
                                OldPartitionNo = pmo != null ? pmo.OldPartitionNo : null,
                                Description = pt != null ? pt.PropertyDescription : null,
                                Type = p.Type ?? (pt != null ? pt.Type : null),
                                Category = pc != null ? pc.PropertyCategoryName : null,
                                UPICId = p.UPICId,
                                OwnerName = p.OwnerName ?? p.OwnerNameEnglish,
                                Address = p.Address ?? p.AddressEnglish,
                                IsCombined = _context.CombinePropertyHistory.Any(c => c.SourcePropertyId == p.Id && c.IsActive)
                            })
                           .FirstOrDefaultAsync(cancellationToken);

        if (result != null)
        {
            var firstDetail = await _context.PropertyDetails
                .AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .OrderBy(x => x.Id)
                .Select(x => x.TypeOfUseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstDetail > 0)
            {
                result.TypeOfUse = await _context.TypeOfUse
                    .AsNoTracking()
                    .Where(x => x.Id == firstDetail && x.IsActive)
                    .Select(x => x.Description)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        return result;
    }

    public async Task AddPropertyMastOldAsync(PropertyMastOldEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.PropertyMastOld.AddAsync(entity, cancellationToken);
    }

    public async Task<PropertyMastOldEntity?> GetPropertyMastOldByIdAsync(int propertyMastOldId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMastOld.FindAsync(new object[] { propertyMastOldId }, cancellationToken);
    }

    public async Task<int> GetFirstOldDetailsIdAsync(int propertyMastOldId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDetailsOld
            .AsNoTracking()
            .Where(x => x.PropertyMastOldId == propertyMastOldId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PropertyDetailsOldEntity?> GetOldDetailsByIdAsync(int oldDetailsId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDetailsOld.FindAsync(new object[] { oldDetailsId }, cancellationToken);
    }

    public async Task AddOldDetailsAsync(PropertyDetailsOldEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.PropertyDetailsOld.AddAsync(entity, cancellationToken);
    }
}
