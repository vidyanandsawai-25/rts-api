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
        // Note: Use projection to avoid querying MarkedForDeletionDate column which doesn't exist
        var assessment = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.PropertyDetailsId)
            .Select(x => new
            {
                x.PropertyDetailsId,
                x.PropertyId,
                x.WingId,
                x.WingNo,
                x.NoOfResidentialToilets,
                x.NoOfCommercialToilets
            })
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
                .Where(x => x.SocietyDetailId == mainResult.Property.SocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
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

    public async Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property == null) return null;

        // Step 2: Validate foreign keys
        var taxZoneExists = await _context.TaxZoneMaster
            .AnyAsync(tz => tz.TaxZoneId == dto.TaxZoneId && tz.IsActive, cancellationToken);
        
        if (!taxZoneExists)
        {
            throw new InvalidOperationException($"TaxZone with ID {dto.TaxZoneId} does not exist or is inactive.");
        }

        var wardExists = await _context.WardMaster
            .AnyAsync(w => w.WardId == dto.WardId && w.IsActive, cancellationToken);
        
        if (!wardExists)
        {
            throw new InvalidOperationException($"Ward with ID {dto.WardId} does not exist or is inactive.");
        }

        // Step 3: Update PropertyMast fields
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

        property.UpdatedDate = DateTime.Now;
        // Step 4: Upsert PropertyMastDetails (assessment)
        var assessmentId = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.PropertyDetailsId)
            .Select(x => x.PropertyDetailsId)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasAssessmentData = dto.WingId.HasValue || dto.WingNo != null || 
                                  dto.NoOfResidentialToilets.HasValue || dto.NoOfCommercialToilets.HasValue;

        if (assessmentId > 0)
        {
            // UPDATE existing record
            var assessment = await _context.PropertyMastDetails.FindAsync(new object[] { assessmentId }, cancellationToken);
            
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
                
                assessment.UpdatedDate = DateTime.Now;
            }
        }
        else if (hasAssessmentData)
        {
            // INSERT new record only if data is provided
            var newAssessment = new PropertyAssessmentEntity
            {
                PropertyId = propertyId,
                WingId = dto.WingId,
                WingNo = dto.WingNo,
                NoOfResidentialToilets = dto.NoOfResidentialToilets,
                NoOfCommercialToilets = dto.NoOfCommercialToilets,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            
            await _context.PropertyMastDetails.AddAsync(newAssessment, cancellationToken);
        }

        // Step 5: Upsert PlotDetails
        var plotId = await _context.PlotDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.PlotId)
            .Select(x => x.PlotId)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasPlotData = dto.PlotArea.HasValue || dto.PlotAreaFtLength.HasValue || 
                           dto.PlotAreaFtWidth.HasValue || dto.PlotAreaMtrLength.HasValue || 
                           dto.PlotAreaMtrWidth.HasValue;

        if (plotId > 0)
        {
            // UPDATE existing record
            var plot = await _context.PlotDetails.FindAsync(new object[] { plotId }, cancellationToken);
            
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
                
                plot.UpdatedDate = DateTime.Now;
            }
        }
        else if (hasPlotData)
        {
            // INSERT new record only if data is provided
            var newPlot = new PlotDetailsEntity
            {
                PropertyId = propertyId,
                PlotArea = dto.PlotArea,
                PlotAreaFtLength = dto.PlotAreaFtLength,
                PlotAreaFtWidth = dto.PlotAreaFtWidth,
                PlotAreaMtrLength = dto.PlotAreaMtrLength,
                PlotAreaMtrWidth = dto.PlotAreaMtrWidth,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            
            await _context.PlotDetails.AddAsync(newPlot, cancellationToken);
        }

        // Step 6: Update SocietyDetailsMast WingId and WingName if provided
        if ((dto.WingId.HasValue || dto.WingName != null) && property.SocietyDetailId.HasValue)
        {
            var society = await _context.SocietyDetailsMast
                .Where(x => x.SocietyDetailId == property.SocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (society != null)
            {
                if (dto.WingId.HasValue)
                    society.WingId = dto.WingId;
                
                if (dto.WingName != null)
                    society.WingName = dto.WingName;
                
                society.UpdatedDate = DateTime.Now;
            }
        }

        // Step 7: Save all changes
        await _context.SaveChangesAsync(cancellationToken);
        
        // Step 8: Return updated data
        return await GetBasicDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // DTO-only flow: Repository returns DTO directly
        // Step 1: Get main property from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Get PropertyMastDetails (assessment) with OwnerTypeId and AdharCardNo
        // Note: PropertyMastDetails has MarkedForDeletion but not MarkedForDeletionDate
        // Project to anonymous type to avoid querying MarkedForDeletionDate column
        var assessment = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.PropertyDetailsId)
            .Select(x => new
            {
                x.PropertyDetailsId,
                x.PropertyId,
                x.OwnerTypeId,
                x.AdharCardNo
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Get OwnerType from OwnerTypeMaster if OwnerTypeId exists
        string? ownerType = null;
        if (assessment?.OwnerTypeId.HasValue == true)
        {
            var ownerTypeMaster = await _context.OwnerTypeMaster
                .Where(x => x.OwnerTypeId == assessment.OwnerTypeId.Value && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            ownerType = ownerTypeMaster?.OwnerType;
        }

        // Build and return DTO
        return new PropertyKycDetailsDto
        {
            PropertyId = property.PropertyId,
            
            // From PropertyMastDetails
            OwnerTypeId = assessment?.OwnerTypeId,
            AdharCardNo = assessment?.AdharCardNo,
            
            // From OwnerTypeMaster
            OwnerType = ownerType,
            
            // From PropertyMast - Owner Information
            OwnerTitle = property.OwnerTitle,
            OwnerName = property.OwnerName,
            OwnerTitleEnglish = property.OwnerTitleEnglish,
            OwnerNameEnglish = property.OwnerNameEnglish,
            
            // From PropertyMast - Occupier Information
            OccupierTitle = property.OccupierTitle,
            OccupierName = property.OccupierName,
            OccupierTitleEnglish = property.OccupierTitleEnglish,
            OccupierNameEnglish = property.OccupierNameEnglish,
            
            // From PropertyMast - Address Information
            Address = property.Address,
            Location = property.Location,
            AddressEnglish = property.AddressEnglish,
            LocationEnglish = property.LocationEnglish,
            
            // From PropertyMast - Flat/Shop Information
            FlatOrShopName = property.FlatOrShopName,
            FlatOrShopNameEnglish = property.FlatOrShopNameEnglish,
            FlatOrShopNo = property.FlatOrShopNo,
            FlatOrShopNoEnglish = property.FlatOrShopNoEnglish,
            
            // From PropertyMast - Contact Information
            MobileNo = property.MobileNo,
            EmailId = property.EmailId
        };
    }

    public async Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property == null) return null;

        // Step 2: Update PropertyMast fields
        if (dto.OwnerTitle != null)
            property.OwnerTitle = dto.OwnerTitle;

        if (dto.OwnerName != null)
            property.OwnerName = dto.OwnerName;

        if (dto.OwnerTitleEnglish != null)
            property.OwnerTitleEnglish = dto.OwnerTitleEnglish;

        if (dto.OwnerNameEnglish != null)
            property.OwnerNameEnglish = dto.OwnerNameEnglish;

        if (dto.OccupierTitle != null)
            property.OccupierTitle = dto.OccupierTitle;

        if (dto.OccupierName != null)
            property.OccupierName = dto.OccupierName;

        if (dto.OccupierTitleEnglish != null)
            property.OccupierTitleEnglish = dto.OccupierTitleEnglish;

        if (dto.OccupierNameEnglish != null)
            property.OccupierNameEnglish = dto.OccupierNameEnglish;

        if (dto.Address != null)
            property.Address = dto.Address;

        if (dto.Location != null)
            property.Location = dto.Location;

        if (dto.AddressEnglish != null)
            property.AddressEnglish = dto.AddressEnglish;

        if (dto.LocationEnglish != null)
            property.LocationEnglish = dto.LocationEnglish;

        if (dto.FlatOrShopName != null)
            property.FlatOrShopName = dto.FlatOrShopName;

        if (dto.FlatOrShopNameEnglish != null)
            property.FlatOrShopNameEnglish = dto.FlatOrShopNameEnglish;

        if (dto.FlatOrShopNo != null)
            property.FlatOrShopNo = dto.FlatOrShopNo;

        if (dto.FlatOrShopNoEnglish != null)
            property.FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish;

        if (dto.MobileNo != null)
            property.MobileNo = dto.MobileNo;

        if (dto.EmailId != null)
            property.EmailId = dto.EmailId;

        property.UpdatedDate = DateTime.Now;

        // Step 3: Upsert PropertyMastDetails (assessment) - OwnerTypeId and AdharCardNo
        var assessmentId = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.PropertyDetailsId)
            .Select(x => x.PropertyDetailsId)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasAssessmentData = dto.OwnerTypeId.HasValue || dto.AdharCardNo != null;

        if (assessmentId > 0)
        {
            // UPDATE existing record
            var assessment = await _context.PropertyMastDetails.FindAsync(new object[] { assessmentId }, cancellationToken);
            
            if (assessment != null)
            {
                if (dto.OwnerTypeId.HasValue)
                    assessment.OwnerTypeId = dto.OwnerTypeId;

                if (dto.AdharCardNo != null)
                    assessment.AdharCardNo = dto.AdharCardNo;
                
                assessment.UpdatedDate = DateTime.Now;
            }
        }
        else if (hasAssessmentData)
        {
            // INSERT new record only if data is provided
            var newAssessment = new PropertyAssessmentEntity
            {
                PropertyId = propertyId,
                OwnerTypeId = dto.OwnerTypeId,
                AdharCardNo = dto.AdharCardNo,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            
            await _context.PropertyMastDetails.AddAsync(newAssessment, cancellationToken);
        }

        // Step 4: Save all changes
        await _context.SaveChangesAsync(cancellationToken);
        
        // Step 5: Return updated data
        return await GetKycDetailsAsync(propertyId, cancellationToken);
    }
    public async Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get property with SocietyDetailId
        var property = await _context.PropertyMast
            .Where(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Get society details with wing master join
        if (!property.SocietyDetailId.HasValue)
        {
            // Return empty DTO if no society details exist
            return new PropertySocietyDetailsDto
            {
                PropertyId = property.PropertyId,
                SocietyDetailId = null
            };
        }

        var societyQuery = from s in _context.SocietyDetailsMast
                           where s.SocietyDetailId == property.SocietyDetailId.Value && s.IsActive && !s.MarkedForDeletion
                           join w in _context.Set<WingEntity>() on s.WingId equals w.WingId into wingJoin
                           from w in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
                           select new PropertySocietyDetailsDto
                           {
                               PropertyId = property.PropertyId,
                               SocietyDetailId = s.SocietyDetailId,
                               WingId = s.WingId,
                               WingNo = w != null ? w.WingNo : null,
                               WingName = s.WingName,
                               SocietyName = s.SocietyName,
                               SocietyAddress = s.SocietyAddress,
                               SecretaryName = s.SecretaryName,
                               ManagerName = s.ManagerName,
                               LandOwnerName = s.LandOwnerName,
                               BuilderName = s.BuilderName,
                               SocietyNameEnglish = s.SocietyNameEnglish,
                               SocietyAddressEnglish = s.SocietyAddressEnglish,
                               SecretaryNameEnglish = s.SecretaryNameEnglish,
                               ManagerNameEnglish = s.ManagerNameEnglish,
                               LandOwnerNameEnglish = s.LandOwnerNameEnglish,
                               BuilderNameEnglish = s.BuilderNameEnglish,
                               ManagerMobileNo = s.ManagerMobileNo,
                               SecretaryMobileNo = s.SecretaryMobileNo,
                               SocietyEmailId = s.SocietyEmailId,
                               SecretaryEmailId = s.SecretaryEmailId,
                               ManagerEmailId = s.ManagerEmailId
                           };

        var result = await societyQuery.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            // Return empty DTO if society details not found
            return new PropertySocietyDetailsDto
            {
                PropertyId = property.PropertyId,
                SocietyDetailId = null
            };
        }

        return result;
    }
    public async Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property == null) return null;

        // Step 2: Validate WingId if provided
        if (dto.WingId.HasValue)
        {
            var wingExists = await _context.Set<WingEntity>()
                .AnyAsync(w => w.WingId == dto.WingId.Value && w.IsActive, cancellationToken);

            if (!wingExists)
            {
                throw new InvalidOperationException($"Wing with ID {dto.WingId.Value} does not exist or is inactive.");
            }
        }

        // Step 3: Get or create society details
        SocietyDetailsEntity? society = null;
        bool needsPropertyUpdate = false;

        if (property.SocietyDetailId.HasValue)
        {
            society = await _context.SocietyDetailsMast
                .FirstOrDefaultAsync(s => s.SocietyDetailId == property.SocietyDetailId.Value && s.IsActive && !s.MarkedForDeletion, cancellationToken);
        }

        if (society == null)
        {
            // Create new society details if none exist or if the referenced one was invalid
            society = new SocietyDetailsEntity
            {
                PropertyId = propertyId,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            _context.SocietyDetailsMast.Add(society);
            needsPropertyUpdate = true;
        }

        // Step 4: Update society details fields
        if (dto.WingId.HasValue)
            society.WingId = dto.WingId;

        if (dto.WingName != null)
            society.WingName = dto.WingName;

        if (dto.SocietyName != null)
            society.SocietyName = dto.SocietyName;

        if (dto.SocietyAddress != null)
            society.SocietyAddress = dto.SocietyAddress;

        if (dto.SecretaryName != null)
            society.SecretaryName = dto.SecretaryName;

        if (dto.ManagerName != null)
            society.ManagerName = dto.ManagerName;

        if (dto.LandOwnerName != null)
            society.LandOwnerName = dto.LandOwnerName;

        if (dto.BuilderName != null)
            society.BuilderName = dto.BuilderName;

        if (dto.SocietyNameEnglish != null)
            society.SocietyNameEnglish = dto.SocietyNameEnglish;

        if (dto.SocietyAddressEnglish != null)
            society.SocietyAddressEnglish = dto.SocietyAddressEnglish;

        if (dto.SecretaryNameEnglish != null)
            society.SecretaryNameEnglish = dto.SecretaryNameEnglish;

        if (dto.ManagerNameEnglish != null)
            society.ManagerNameEnglish = dto.ManagerNameEnglish;

        if (dto.LandOwnerNameEnglish != null)
            society.LandOwnerNameEnglish = dto.LandOwnerNameEnglish;

        if (dto.BuilderNameEnglish != null)
            society.BuilderNameEnglish = dto.BuilderNameEnglish;

        if (dto.ManagerMobileNo != null)
            society.ManagerMobileNo = dto.ManagerMobileNo;

        if (dto.SecretaryMobileNo != null)
            society.SecretaryMobileNo = dto.SecretaryMobileNo;

        if (dto.SocietyEmailId != null)
            society.SocietyEmailId = dto.SocietyEmailId;

        if (dto.SecretaryEmailId != null)
            society.SecretaryEmailId = dto.SecretaryEmailId;

        if (dto.ManagerEmailId != null)
            society.ManagerEmailId = dto.ManagerEmailId;

        society.UpdatedDate = DateTime.Now;

        // Step 5: Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        // Update property's SocietyDetailId if a new society was created or the reference was invalid
        if (needsPropertyUpdate)
        {
            property.SocietyDetailId = society.SocietyDetailId;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Step 6: Return updated data
        return await GetSocietyDetailsAsync(propertyId, cancellationToken);
    }
}
