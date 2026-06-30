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
        // Step 1: Get PropertyMastOldId from PropertyMast — read-only projection.
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
            return new PropertyOldDetailsDto { PropertyId = propertyId };

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Get PropertyMastOld data
        var oldMastData = await _context.PropertyMastOld
            .AsNoTracking()
            .Where(x => x.Id == propertyMastOldId && x.IsActive && !x.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Get first PropertyDetailsOld data (or aggregate if needed)
        var oldDetailsData = await _context.PropertyDetailsOld
            .AsNoTracking()
            .Where(x => x.PropertyMastOldId == propertyMastOldId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 4: Calculate OldTotalTax and OldGeneralTax from TransMastOld if exists, otherwise use PropertyMastOld values
        double? oldTotalTax = null;
        double? oldGeneralTax = null;
        var transMastOldExists = await _context.TransMastOld
            .AnyAsync(t => t.PropertyMastOldId == propertyMastOldId && t.IsActive && !t.MarkedForDeletion, cancellationToken);

        if (transMastOldExists)
        {
            // Get all taxes to identify Interest and General Tax
            var oldTaxes = await _context.TaxMaster
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

            // Calculate Total Tax (excluding Interest)
            var totalTaxFromTransMastOld = await _context.TransMastOld
                .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                           t.IsActive &&
                           !t.MarkedForDeletion &&
                           (!interestTaxId.HasValue || t.TaxId != interestTaxId.Value))
                .SumAsync(t => (double?)t.TaxAmount, cancellationToken);

            oldTotalTax = totalTaxFromTransMastOld;

            // Calculate General Tax
            if (generalTaxId.HasValue)
            {
                var generalTaxFromTransMastOld = await _context.TransMastOld
                    .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                               t.IsActive &&
                               !t.MarkedForDeletion &&
                               t.TaxId == generalTaxId.Value)
                    .SumAsync(t => (double?)t.TaxAmount, cancellationToken);

                oldGeneralTax = generalTaxFromTransMastOld;
            }
            else
            {
                // If General Tax is not configured in TaxMaster, use PropertyMastOld value
                oldGeneralTax = oldMastData?.OldGeneralTax;
            }
        }
        else
        {
            oldTotalTax = oldMastData?.OldTotalTax;
            oldGeneralTax = oldMastData?.OldGeneralTax;
        }

        // Build and return DTO
        return new PropertyOldDetailsDto
        {
            PropertyId = propertyId,
            // From PropertyMastOld
            OldWardNo = oldMastData?.OldWardNo,
            OldPropertyNo = oldMastData?.OldPropertyNo,
            OldPartitionNo = oldMastData?.OldPartitionNo,
            OldEgovNo = oldMastData?.OldEgovNo,
            OldPlotArea = oldMastData?.OldPlotArea != null ? Math.Round(oldMastData.OldPlotArea.Value, 2) : null,
            OldPlotNo = oldMastData?.OldPlotNo,
            OldRV = oldMastData?.OldRV != null ? Math.Round(oldMastData.OldRV.Value, 2) : null,
            OldALV = oldMastData?.OldALV != null ? Math.Round(oldMastData.OldALV.Value, 2) : null,
            OldTotalTax = oldTotalTax != null ? Math.Round(oldTotalTax.Value, 2) : null,
            OldZoneNo = oldMastData?.OldZoneNo,
            OldGeneralTax = oldGeneralTax != null ? Math.Round(oldGeneralTax.Value, 2) : null,
            OldCSN = oldMastData?.OldCSN,
            OldConstructionArea = oldMastData?.OldConstructionArea != null ? Math.Round(oldMastData.OldConstructionArea.Value, 2) : null,
            // From PropertyDetailsOld
            OldConstructionYear = oldDetailsData?.OldConstructionYear,
            OldCarpetAreaSqFeet = oldDetailsData?.OldCarpetAreaSqFeet != null ? Math.Round(oldDetailsData.OldCarpetAreaSqFeet.Value, 2) : null,
            OldCarpetAreaSqMeter = oldDetailsData?.OldCarpetAreaSqMeter != null ? Math.Round(oldDetailsData.OldCarpetAreaSqMeter.Value, 2) : null,
            OldConstructionTypeId = oldDetailsData?.OldConstructionTypeId,
            OldTypeOfUseId = oldDetailsData?.OldTypeOfUseId
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
                                Address = p.Address ?? p.AddressEnglish
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
