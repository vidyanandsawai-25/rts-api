using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Specialized repository implementation for Property entity
/// Provides custom query methods for property-related operations
/// </summary>
public class PropertyRepository : Repository<PropertyEntity, int>, IPropertyRepository
{
    public PropertyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // DTO-only flow: Repository returns DTO directly
        // Using simple separate queries approach (EF Core compatible)
        // Step 1: Get main property with master data joins
        var mainQuery = from p in _context.PropertyMast
                        where p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion
                        
                        join w in _context.WardMaster on p.WardId equals w.WardId into wardJoin
                        from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()
                        
                        join z in _context.ZoneMaster on (w != null ? w.ZoneId : (int?)null) equals z.ZoneId into zoneJoin
                        from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()
                        
                        join tz in _context.TaxZoneMaster on p.TaxZoneId equals tz.TaxZoneId into taxZoneJoin
                        from tz in taxZoneJoin.Where(x => x.IsActive).DefaultIfEmpty()
                        
                        join pc in _context.PropertyCategory on p.CategoryId equals pc.PropertyCategoryId into categoryJoin
                        from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
                        
                        join pt in _context.PropertyTypeMaster on p.PropertyTypeId equals pt.PropertyTypeId into typeJoin
                        from pt in typeJoin.Where(x => x.IsActive).DefaultIfEmpty()
                        
                        select new
                        {
                            Property = p,
                            Ward = w,
                            Zone = z,
                            TaxZone = tz,
                            Category = pc,
                            PropertyType = pt
                        };

        var mainResult = await mainQuery.FirstOrDefaultAsync(cancellationToken);
        
        if (mainResult == null)
            return null;

        // Step 2: Get first PropertyMastDetails (assessment)
        var assessment = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.PropertyDetailsId)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Sum PropertyDetails (includes both sqm and sqft)
        var detailsSum = await _context.PropertyDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .GroupBy(x => x.PropertyId)
            .Select(g => new
            {
                TotalCarpetAreaSqMeter = g.Sum(x => x.CarpetAreaSqMeter) ?? 0,
                TotalBuiltupAreaSqMeter = g.Sum(x => x.BuiltupAreaSqMeter) ?? 0,
                TotalCarpetAreaSqFeet = g.Sum(x => x.CarpetAreaSqFeet),
                TotalBuiltupAreaSqFeet = g.Sum(x => x.BuiltupAreaSqFeet)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 4: Get first PlotDetails (includes ft and mtr dimensions)
        var plot = await _context.PlotDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.PlotId)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 5: Get SocietyDetails WingId
        var society = mainResult.Property.SocietyDetailId.HasValue
            ? await _context.SocietyDetailsMast
                .Where(x => x.SocietyDetailId == mainResult.Property.SocietyDetailId.Value && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Build and return DTO
        return new PropertyBasicDetailsDto
        {
            PropertyId = mainResult.Property.PropertyId,
            WardId = mainResult.Property.WardId,
            WardNo = mainResult.Ward?.WardNo,
            ZoneId = mainResult.Ward?.ZoneId,
            Division = mainResult.Zone?.Description,
            PropertyNo = mainResult.Property.PropertyNo,
            PartitionNo = mainResult.Property.PartitionNo,
            FlatOrShopNo = mainResult.Property.FlatOrShopNo,
            PlotNo = mainResult.Property.PlotNo,
            SurveyNo = mainResult.Property.CSN,
            TaxZoneId = mainResult.Property.TaxZoneId,
            TaxZoneNo = mainResult.TaxZone?.TaxZoneNo,
            CategoryId = mainResult.Property.CategoryId,
            CategoryName = mainResult.Category?.PropertyCategoryName,
            PropertyTypeId = mainResult.Property.PropertyTypeId,
            PropertyDescription = mainResult.PropertyType?.PropertyDescription,
            UPICId = mainResult.Property.UPICId,
            SubZoneNo = mainResult.Property.SubZoneNo,
            WingNo = assessment?.WingNo,
            NoOfResidentialToilets = assessment?.NoOfResidentialToilets,
            NoOfCommercialToilets = assessment?.NoOfCommercialToilets,
            TotalCarpetAreaSqMeter = detailsSum?.TotalCarpetAreaSqMeter ?? 0,
            TotalBuiltupAreaSqMeter = detailsSum?.TotalBuiltupAreaSqMeter ?? 0,
            TotalCarpetAreaSqFeet = detailsSum?.TotalCarpetAreaSqFeet,
            TotalBuiltupAreaSqFeet = detailsSum?.TotalBuiltupAreaSqFeet,
            PlotArea = plot?.PlotArea,
            PlotAreaFtLength = plot?.PlotAreaFtLength,
            PlotAreaFtWidth = plot?.PlotAreaFtWidth,
            PlotAreaMtrLength = plot?.PlotAreaMtrLength,
            PlotAreaMtrWidth = plot?.PlotAreaMtrWidth,
            WingId = society?.WingId ?? assessment?.WingId,
            WingName = society?.WingName
        };
    }

    public async Task<bool> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property == null) return false;

        // Update PropertyMast fields
        property.WardId = dto.WardId;
        property.TaxZoneId = dto.TaxZoneId;

        if (dto.CategoryId.HasValue)
            property.CategoryId = dto.CategoryId.Value;

        if (dto.PropertyTypeId.HasValue)
            property.PropertyTypeId = dto.PropertyTypeId.Value;

        if (dto.PartitionNo != null)
            property.PartitionNo = dto.PartitionNo;

        if (dto.FlatOrShopNo != null)
            property.FlatOrShopNo = dto.FlatOrShopNo;

        if (dto.PlotNo != null)
            property.PlotNo = dto.PlotNo;

        if (dto.SurveyNo != null)
            property.CSN = dto.SurveyNo;

        if (dto.UPICId != null)
            property.UPICId = dto.UPICId;

        if (dto.SubZoneNo != null)
            property.SubZoneNo = dto.SubZoneNo;
        // Update PropertyMastDetails (assessment)
        var assessment = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.PropertyDetailsId)
            .FirstOrDefaultAsync(cancellationToken);

        if (assessment != null)
        {
            if (dto.WingId.HasValue)
                assessment.WingId = dto.WingId;
            
            if (dto.WingNo != null)
                assessment.WingNo = dto.WingNo;
            
            if (dto.NoOfResidentialToilets.HasValue)
                assessment.NoOfResidentialToilets = dto.NoOfResidentialToilets;
            
            if (dto.NoOfCommercialToilets.HasValue)
                assessment.NoOfCommercialToilets = dto.NoOfCommercialToilets;
        }

        // Update PlotDetails
        var plot = await _context.PlotDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.PlotId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plot != null)
        {
            if (dto.PlotArea.HasValue)
                plot.PlotArea = dto.PlotArea;
            
            if (dto.PlotAreaFtLength.HasValue)
                plot.PlotAreaFtLength = dto.PlotAreaFtLength;
            
            if (dto.PlotAreaFtWidth.HasValue)
                plot.PlotAreaFtWidth = dto.PlotAreaFtWidth;
            
            if (dto.PlotAreaMtrLength.HasValue)
                plot.PlotAreaMtrLength = dto.PlotAreaMtrLength;
            
            if (dto.PlotAreaMtrWidth.HasValue)
                plot.PlotAreaMtrWidth = dto.PlotAreaMtrWidth;
        }

        // Update SocietyDetailsMast WingId and WingName if provided
        if ((dto.WingId.HasValue || dto.WingName != null) && property.SocietyDetailId.HasValue)
        {
            var society = await _context.SocietyDetailsMast
                .Where(x => x.SocietyDetailId == property.SocietyDetailId.Value && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (society != null)
            {
                if (dto.WingId.HasValue)
                    society.WingId = dto.WingId;
                
                if (dto.WingName != null)
                    society.WingName = dto.WingName;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
