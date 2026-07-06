using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Data-access implementation for the Property "Basic Details" tab.
/// Contains only persistence concerns (queries, tracked loads and staged inserts). Business rules
/// and the unit-of-work/SaveChanges call live in <c>PropertyBasicDetailsService</c>.
/// </summary>
public class PropertyBasicDetailsRepository : PropertyRepositoryBase, IPropertyBasicDetailsRepository
{
    public PropertyBasicDetailsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // DTO-only flow: Repository returns DTO directly
        // Using simple separate queries approach (EF Core compatible)
        // Step 1: Get main property with master data joins
        // Read-only projection: AsNoTracking on the root source makes the entire composed query non-tracking.
        var mainQuery = from p in _context.PropertyMast.AsNoTracking()
                        where p.Id == propertyId && p.IsActive && !p.MarkedForDeletion

                        join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                        from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join z in _context.ZoneMaster.AsNoTracking() on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                        from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join tz in _context.TaxZoneMaster.AsNoTracking() on p.TaxZoneId equals tz.Id into taxZoneJoin
                        from tz in taxZoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                        from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into typeJoin
                        from pt in typeJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join m in _context.MoujaEntity.AsNoTracking() on p.MoujaId equals m.Id into moujaJoin
                        from m in moujaJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        select new
                        {
                            Property = p,
                            Ward = w,
                            Zone = z,
                            TaxZone = tz,
                            Category = pc,
                            PropertyType = pt,
                            Mouja = m
                        };

        var mainResult = await mainQuery.FirstOrDefaultAsync(cancellationToken);

        if (mainResult == null)
            return null;

        // Step 2: Get first PropertyMastDetails (assessment) — read-only projection.
        var assessment = await _context.PropertyMastDetails
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                PropertyDetailsId = x.Id,
                x.PropertyId,
                x.NoOfResidentialToilets,
                x.NoOfCommercialToilets,
                x.Latitude,
                x.Longitude
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Sum PropertyDetails (includes both sqm and sqft) — read-only aggregation.
        var detailsSum = await _context.PropertyDetails
            .AsNoTracking()
.Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(g => new
            {
                TotalCarpetAreaSqMeter = g.Sum(x => x.CarpetAreaSqMeter) ?? 0,
                TotalBuiltupAreaSqMeter = g.Sum(x => x.BuiltupAreaSqMeter) ?? 0,
                TotalCarpetAreaSqFeet = g.Sum(x => x.CarpetAreaSqFeet),
                TotalBuiltupAreaSqFeet = g.Sum(x => x.BuiltupAreaSqFeet)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 4: Get first PlotDetails — project only the area columns needed for the DTO.
        var plot = await _context.PlotDetails
            .AsNoTracking()
.Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.PlotArea,
                x.PlotAreaFtLength,
                x.PlotAreaFtWidth,
                x.PlotAreaMtrLength,
                x.PlotAreaMtrWidth
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 5: Project only the wing-related columns needed from SocietyDetails.
        var society = mainResult.Property.SocietyDetailId.HasValue
            ? await _context.SocietyDetailsMast
                .AsNoTracking()
                .Where(x => x.Id == mainResult.Property.SocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new { x.WingId, x.WingName })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Resolve WingNo from society.WingId lookup (already projecting only WingNo).
        string? wingNo = null;
        if (society?.WingId.HasValue == true)
        {
            wingNo = await _context.Set<WingEntity>()
                .AsNoTracking()
                .Where(w => w.Id == society.WingId && w.IsActive)
                .Select(w => w.WingNo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Retrieve RateSectionDescription if WardId is set
        string? rateSectionDescription = null;
        if (mainResult.Property.WardId > 0)
        {
            rateSectionDescription = await (
                from rsd in _context.RateSectionDetails.AsNoTracking()
                where rsd.WardId == mainResult.Property.WardId && rsd.IsActive
                join rs in _context.RateSection.AsNoTracking() on rsd.RateSectionId equals rs.Id
                where rs.IsActive
                select rs.Description
            ).FirstOrDefaultAsync(cancellationToken);
        }

        // Retrieve the earliest active construction year for the property
        var constructionYear = await _context.PropertyDetails
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion &&
                        !string.IsNullOrEmpty(x.ConstructionYear) &&
                        x.ConstructionYear.Length == 4)
            .OrderBy(x => x.ConstructionYear)
            .Select(x => x.ConstructionYear)
            .FirstOrDefaultAsync(cancellationToken);

        // Build and return DTO
        return new PropertyBasicDetailsDto
        {
            PropertyId = mainResult.Property.Id,
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
            MoujaId = mainResult.Property.MoujaId,
            MoujaName = mainResult.Mouja?.MoujaName,
            WingNo = wingNo,
            NoOfResidentialToilets = assessment?.NoOfResidentialToilets,
            NoOfCommercialToilets = assessment?.NoOfCommercialToilets,
            TotalCarpetAreaSqMeter = Math.Round(detailsSum?.TotalCarpetAreaSqMeter ?? 0, 2),
            TotalBuiltupAreaSqMeter = Math.Round(detailsSum?.TotalBuiltupAreaSqMeter ?? 0, 2),
            TotalCarpetAreaSqFeet = detailsSum?.TotalCarpetAreaSqFeet != null ? Math.Round(detailsSum.TotalCarpetAreaSqFeet.Value, 2) : null,
            TotalBuiltupAreaSqFeet = detailsSum?.TotalBuiltupAreaSqFeet != null ? Math.Round(detailsSum.TotalBuiltupAreaSqFeet.Value, 2) : null,
            PlotArea = plot?.PlotArea != null ? Math.Round(plot.PlotArea.Value, 2) : null,
            PlotAreaFtLength = plot?.PlotAreaFtLength != null ? Math.Round(plot.PlotAreaFtLength.Value, 2) : null,
            PlotAreaFtWidth = plot?.PlotAreaFtWidth != null ? Math.Round(plot.PlotAreaFtWidth.Value, 2) : null,
            PlotAreaMtrLength = plot?.PlotAreaMtrLength != null ? Math.Round(plot.PlotAreaMtrLength.Value, 2) : null,
            PlotAreaMtrWidth = plot?.PlotAreaMtrWidth != null ? Math.Round(plot.PlotAreaMtrWidth.Value, 2) : null,
            WingId = society?.WingId,
            WingName = society?.WingName,
            RateSectionDescription = rateSectionDescription,
            Latitude = assessment?.Latitude,
            Longitude = assessment?.Longitude,
            ConstructionYear = constructionYear
        };
    }

    public async Task<int> GetFirstAssessmentIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PropertyAssessmentEntity?> GetAssessmentByIdAsync(int assessmentId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMastDetails.FindAsync(new object[] { assessmentId }, cancellationToken);
    }

    public async Task AddAssessmentAsync(PropertyAssessmentEntity assessment, CancellationToken cancellationToken = default)
    {
        await _context.PropertyMastDetails.AddAsync(assessment, cancellationToken);
    }

    public async Task<int> GetFirstPlotIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PlotDetails
.Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PlotDetailsEntity?> GetPlotByIdAsync(int plotId, CancellationToken cancellationToken = default)
    {
        return await _context.PlotDetails.FindAsync(new object[] { plotId }, cancellationToken);
    }

    public async Task AddPlotAsync(PlotDetailsEntity plot, CancellationToken cancellationToken = default)
    {
        await _context.PlotDetails.AddAsync(plot, cancellationToken);
    }

    public async Task<SocietyDetailsEntity?> GetSocietyByIdAsync(int societyId, CancellationToken cancellationToken = default)
    {
        return await _context.SocietyDetailsMast
            .FirstOrDefaultAsync(s => s.Id == societyId && s.IsActive && !s.MarkedForDeletion, cancellationToken);
    }

    public async Task<SocietyDetailsEntity?> GetSocietyByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.SocietyDetailsMast
            .FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion, cancellationToken);
    }

    public void AddSociety(SocietyDetailsEntity society)
    {
        _context.SocietyDetailsMast.Add(society);
    }

    public async Task<WingEntity?> GetActiveWingByNoAsync(string wingNo, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WingEntity>()
            .FirstOrDefaultAsync(w => w.WingNo == wingNo && w.IsActive, cancellationToken);
    }
}
