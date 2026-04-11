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
                        where p.Id == propertyId && p.IsActive && !p.MarkedForDeletion

                        join w in _context.WardMaster on p.WardId equals w.Id into wardJoin
                        from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join z in _context.ZoneMaster on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                        from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join tz in _context.TaxZoneMaster on p.TaxZoneId equals tz.Id into taxZoneJoin
                        from tz in taxZoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join pc in _context.PropertyCategoryMaster on p.CategoryId equals pc.Id into categoryJoin
                        from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join pt in _context.PropertyTypeMaster on p.PropertyTypeId equals pt.Id into typeJoin
                        from pt in typeJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join m in _context.MoujaEntity on p.MoujaId equals m.Id into moujaJoin
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

        // Step 2: Get first PropertyMastDetails (assessment)
        var assessment = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                PropertyDetailsId = x.Id,
                x.Id,
                x.PropertyId,
                x.NoOfResidentialToilets,
                x.NoOfCommercialToilets,
                x.WingNo
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
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 5: Get SocietyDetails WingId and resolve WingNo
        var society = mainResult.Property.SocietyDetailId.HasValue
            ? await _context.SocietyDetailsMast
                .Where(x => x.Id == mainResult.Property.SocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Resolve WingNo: Priority is society.WingId lookup, then fallback to assessment.WingNo
        string? wingNo = null;
        if (society?.WingId.HasValue == true)
        {
            wingNo = await _context.Set<WingEntity>()
                .Where(w => w.Id == society.WingId && w.IsActive)
                .Select(w => w.WingNo)
                .FirstOrDefaultAsync(cancellationToken);
        }
        // Fallback to assessment WingNo if not found via society
        if (wingNo == null)
        {
            wingNo = assessment?.WingNo;
        }

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
            TotalCarpetAreaSqMeter = detailsSum?.TotalCarpetAreaSqMeter ?? 0,
            TotalBuiltupAreaSqMeter = detailsSum?.TotalBuiltupAreaSqMeter ?? 0,
            TotalCarpetAreaSqFeet = detailsSum?.TotalCarpetAreaSqFeet,
            TotalBuiltupAreaSqFeet = detailsSum?.TotalBuiltupAreaSqFeet,
            PlotArea = plot?.PlotArea,
            PlotAreaFtLength = plot?.PlotAreaFtLength,
            PlotAreaFtWidth = plot?.PlotAreaFtWidth,
            PlotAreaMtrLength = plot?.PlotAreaMtrLength,
            PlotAreaMtrWidth = plot?.PlotAreaMtrWidth,
            WingId = society?.WingId,
            WingName = society?.WingName
        };
    }

    public async Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property == null) return null;

        // Step 2: Validate foreign keys
        var taxZoneExists = await _context.TaxZoneMaster
            .AnyAsync(tz => tz.Id == dto.TaxZoneId && tz.IsActive, cancellationToken);

        if (!taxZoneExists)
        {
            throw new InvalidOperationException($"TaxZone with ID {dto.TaxZoneId} does not exist or is inactive.");
        }

        var wardExists = await _context.WardMaster
            .AnyAsync(w => w.Id == dto.WardId && w.IsActive, cancellationToken);

        if (!wardExists)
        {
            throw new InvalidOperationException($"Ward with ID {dto.WardId} does not exist or is inactive.");
        }

        // Validate MoujaId if provided
        if (dto.MoujaId.HasValue)
        {
            var moujaExists = await _context.MoujaEntity
                .AnyAsync(m => m.Id == dto.MoujaId.Value && m.IsActive, cancellationToken);

            if (!moujaExists)
            {
                throw new InvalidOperationException($"Mouja with ID {dto.MoujaId.Value} does not exist or is inactive.");
            }
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

        if (dto.MoujaId.HasValue)
            property.MoujaId = dto.MoujaId.Value;

        property.UpdatedDate = DateTime.Now;
        
        // Step 4: Upsert PropertyMastDetails (assessment) - includes NoOfToilets and WingNo fields
        var assessmentId = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasAssessmentData = dto.NoOfResidentialToilets.HasValue || dto.NoOfCommercialToilets.HasValue || dto.WingNo != null;

        if (assessmentId > 0)
        {
            // UPDATE existing record
            var assessment = await _context.PropertyMastDetails.FindAsync(new object[] { assessmentId }, cancellationToken);

            if (assessment != null)
            {
                if (dto.NoOfResidentialToilets.HasValue)
                    assessment.NoOfResidentialToilets = dto.NoOfResidentialToilets;

                if (dto.NoOfCommercialToilets.HasValue)
                    assessment.NoOfCommercialToilets = dto.NoOfCommercialToilets;

                if (dto.WingNo != null)
                    assessment.WingNo = dto.WingNo;

                assessment.UpdatedDate = DateTime.Now;
            }
        }
        else if (hasAssessmentData)
        {
            // INSERT new record only if data is provided
            var newAssessment = new PropertyAssessmentEntity
            {
                PropertyId = propertyId,
                NoOfResidentialToilets = dto.NoOfResidentialToilets,
                NoOfCommercialToilets = dto.NoOfCommercialToilets,
                WingNo = dto.WingNo,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };

            await _context.PropertyMastDetails.AddAsync(newAssessment, cancellationToken);
        }

        // Step 5: Upsert PlotDetails
        var plotId = await _context.PlotDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
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

        // Step 6: Upsert SocietyDetailsMast for WingId, WingNo, and WingName if provided
        if (dto.WingId.HasValue || dto.WingName != null || dto.WingNo != null)
        {
            SocietyDetailsEntity? society = null;

            // First, try to find existing society by SocietyDetailId from property
            if (property.SocietyDetailId.HasValue)
            {
                society = await _context.SocietyDetailsMast
                    .FirstOrDefaultAsync(s => s.Id == property.SocietyDetailId.Value && s.IsActive && !s.MarkedForDeletion, cancellationToken);
            }

            // If not found by SocietyDetailId, try to find by PropertyId
            if (society == null)
            {
                society = await _context.SocietyDetailsMast
                    .FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion, cancellationToken);
                
                // Link the society to the property if found
                if (society != null && !property.SocietyDetailId.HasValue)
                {
                    property.SocietyDetailId = society.Id;
                }
            }

            // Create new society if still not found
            if (society == null)
            {
                society = new SocietyDetailsEntity
                {
                    PropertyId = propertyId,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                _context.SocietyDetailsMast.Add(society);
                
                // Will need to link property after save
                await _context.SaveChangesAsync(cancellationToken);
                property.SocietyDetailId = society.Id;
            }

            if (dto.WingId.HasValue)
                society.WingId = dto.WingId;

            if (dto.WingName != null)
                society.WingName = dto.WingName;

            if (dto.WingNo != null)
            {
                // If you want to link to a WingEntity by number, set the reference
                var wing = await _context.Set<WingEntity>().FirstOrDefaultAsync(w => w.WingNo == dto.WingNo && w.IsActive, cancellationToken);
                if (wing != null)
                {
                    society.WingId = wing.Id;
                }
            }

            society.UpdatedDate = DateTime.Now;
        }

        // Step 7: Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        // Step 8: Return updated data
        return await GetBasicDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get property with SocietyDetailId
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Get society details with wing master join
        if (!property.SocietyDetailId.HasValue)
        {
            // Return empty DTO if no society details exist
            return new PropertySocietyDetailsDto
            {
                PropertyId = property.Id,
                SocietyDetailId = null
            };
        }

        var societyQuery = from s in _context.SocietyDetailsMast
                           where s.Id == property.SocietyDetailId.Value && s.IsActive && !s.MarkedForDeletion
                           join w in _context.Set<WingEntity>() on s.WingId equals w.Id into wingJoin
                           from w in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
                           select new PropertySocietyDetailsDto
                           {
                               PropertyId = property.Id,
                               SocietyDetailId = s.Id,
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
                PropertyId = property.Id,
                SocietyDetailId = null
            };
        }

        return result;
    }

    public async Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property == null) return null;

        // Step 2: Validate WingId if provided
        if (dto.WingId.HasValue)
        {
            var wingExists = await _context.Set<WingEntity>()
                .AnyAsync(w => w.Id == dto.WingId.Value && w.IsActive, cancellationToken);

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
                .FirstOrDefaultAsync(s => s.Id == property.SocietyDetailId.Value && s.IsActive && !s.MarkedForDeletion, cancellationToken);
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
            property.SocietyDetailId = society.Id;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Step 6: Return updated data
        return await GetSocietyDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // DTO-only flow: Repository returns DTO directly
        // Step 1: Get main property from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Get PropertyMastDetails (assessment) with OwnerTypeId and AdharCardNo
        // Note: PropertyMastDetails has MarkedForDeletion but not MarkedForDeletionDate
        // Project to anonymous type to avoid querying MarkedForDeletionDate column
        var assessment = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                PropertyDetailsId = x.Id,
                x.Id,
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
                .Where(x => x.Id == assessment.OwnerTypeId.Value && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            ownerType = ownerTypeMaster?.OwnerType;
        }

        // Build and return DTO
        return new PropertyKycDetailsDto
        {
            PropertyId = property.Id,

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
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

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
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
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

    public async Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Check if property exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 1: Get PropertyMastOld data
        var oldMastData = await _context.PropertyMastOld
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 2: Get first PropertyDetailsOld data (or aggregate if needed)
        var oldDetailsData = await _context.PropertyDetailsOld
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Build and return DTO
        return new PropertyOldDetailsDto
        {
            PropertyId = propertyId,
            // From PropertyMastOld
            OldWardNo = oldMastData?.OldWardNo,
            OldPropertyNo = oldMastData?.OldPropertyNo,
            OldPartitionNo = oldMastData?.OldPartitionNo,
            OldEgovNo = oldMastData?.OldEgovNo,
            OldPlotArea = oldMastData?.OldPlotArea,
            OldPlotNo = oldMastData?.OldPlotNo,
            OldRV = oldMastData?.OldRV,
            OldALV = oldMastData?.OldALV,
            OldTotalTax = oldMastData?.OldTotalTax,
            OldZoneNo = oldMastData?.OldZoneNo,
            // From PropertyDetailsOld
            OldConstructionYear = oldDetailsData?.OldConstructionYear,
            OldCarpetAreaSqFeet = oldDetailsData?.OldCarpetAreaSqfeet,
            OldCarpetAreaSqMeter = oldDetailsData?.OldCarpetAreaSqMeter,
            OldRegistration = oldDetailsData?.OldRegistration,
            OldConstructionTypeId = oldDetailsData?.OldConstructionTypeId,
            OldTypeOfUseId = oldDetailsData?.OldTypeOfUseId
        };
    }

    public async Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Upsert PropertyMastOld
        var oldMastId = await _context.PropertyMastOld
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasOldMastData = dto.OldWardNo != null || dto.OldPropertyNo != null ||
                              dto.OldPartitionNo != null || dto.OldEgovNo != null ||
                              dto.OldPlotArea.HasValue || dto.OldPlotNo != null ||
                              dto.OldRV.HasValue || dto.OldALV.HasValue ||
                              dto.OldTotalTax.HasValue || dto.OldZoneNo != null;

        if (oldMastId > 0)
        {
            // UPDATE existing record
            var oldMastData = await _context.PropertyMastOld.FindAsync(new object[] { oldMastId }, cancellationToken);

            if (oldMastData != null)
            {
                if (dto.OldWardNo != null)
                    oldMastData.OldWardNo = dto.OldWardNo;

                if (dto.OldPropertyNo != null)
                    oldMastData.OldPropertyNo = dto.OldPropertyNo;

                if (dto.OldPartitionNo != null)
                    oldMastData.OldPartitionNo = dto.OldPartitionNo;

                if (dto.OldEgovNo != null)
                    oldMastData.OldEgovNo = dto.OldEgovNo;

                if (dto.OldPlotArea.HasValue)
                    oldMastData.OldPlotArea = dto.OldPlotArea;

                if (dto.OldPlotNo != null)
                    oldMastData.OldPlotNo = dto.OldPlotNo;

                if (dto.OldRV.HasValue)
                    oldMastData.OldRV = dto.OldRV;

                if (dto.OldALV.HasValue)
                    oldMastData.OldALV = dto.OldALV;

                if (dto.OldTotalTax.HasValue)
                    oldMastData.OldTotalTax = dto.OldTotalTax;

                if (dto.OldZoneNo != null)
                    oldMastData.OldZoneNo = dto.OldZoneNo;

                oldMastData.UpdatedDate = DateTime.Now;
            }
        }
        else if (hasOldMastData)
        {
            // INSERT new record only if data is provided
            var newOldMastData = new PropertyMastOldEntity
            {
                PropertyId = propertyId,
                OldWardNo = dto.OldWardNo,
                OldPropertyNo = dto.OldPropertyNo,
                OldPartitionNo = dto.OldPartitionNo,
                OldEgovNo = dto.OldEgovNo,
                OldPlotArea = dto.OldPlotArea,
                OldPlotNo = dto.OldPlotNo,
                OldRV = dto.OldRV,
                OldALV = dto.OldALV,
                OldTotalTax = dto.OldTotalTax,
                OldZoneNo = dto.OldZoneNo,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            await _context.PropertyMastOld.AddAsync(newOldMastData, cancellationToken);
        }

        // Step 3: Upsert PropertyDetailsOld
        var oldDetailsId = await _context.PropertyDetailsOld
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasOldDetailsData = dto.OldConstructionYear != null || dto.OldCarpetAreaSqFeet.HasValue ||
                                 dto.OldCarpetAreaSqMeter.HasValue || dto.OldRegistration.HasValue ||
                                 dto.OldConstructionTypeId != null || dto.OldTypeOfUseId != null;

        if (oldDetailsId > 0)
        {
            // UPDATE existing record
            var oldDetailsData = await _context.PropertyDetailsOld.FindAsync(new object[] { oldDetailsId }, cancellationToken);

            if (oldDetailsData != null)
            {
                if (dto.OldConstructionYear != null)
                    oldDetailsData.OldConstructionYear = dto.OldConstructionYear;

                if (dto.OldCarpetAreaSqFeet.HasValue)
                    oldDetailsData.OldCarpetAreaSqfeet = dto.OldCarpetAreaSqFeet;

                if (dto.OldCarpetAreaSqMeter.HasValue)
                    oldDetailsData.OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter;

                if (dto.OldRegistration.HasValue)
                    oldDetailsData.OldRegistration = dto.OldRegistration;

                if (dto.OldConstructionTypeId != null)
                    oldDetailsData.OldConstructionTypeId = dto.OldConstructionTypeId;

                if (dto.OldTypeOfUseId != null)
                    oldDetailsData.OldTypeOfUseId = dto.OldTypeOfUseId;

                oldDetailsData.UpdatedDate = DateTime.Now;
            }
        }
        else if (hasOldDetailsData)
        {
            // INSERT new record only if data is provided
            var newOldDetailsData = new PropertyDetailsOldEntity
            {
                PropertyId = propertyId,
                OldConstructionYear = dto.OldConstructionYear,
                OldCarpetAreaSqfeet = dto.OldCarpetAreaSqFeet,
                OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter,
                OldRegistration = dto.OldRegistration,
                OldConstructionTypeId = dto.OldConstructionTypeId,
                OldTypeOfUseId = dto.OldTypeOfUseId,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            await _context.PropertyDetailsOld.AddAsync(newOldDetailsData, cancellationToken);
        }

        // Step 4: Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        // Step 5: Return updated data
        return await GetOldDetailsAsync(propertyId, cancellationToken);
    }
}
