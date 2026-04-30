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

                        join pt in _context.PropertyTypeMasters on p.PropertyTypeId equals pt.Id into typeJoin
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
                x.PropertyId,
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
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 5: Get SocietyDetails WingId and resolve WingNo
        var society = mainResult.Property.SocietyDetailId.HasValue
            ? await _context.SocietyDetailsMast
                .Where(x => x.Id == mainResult.Property.SocietyDetailId.Value && x.IsActive && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Resolve WingNo from society.WingId lookup
        string? wingNo = null;
        if (society?.WingId.HasValue == true)
        {
            wingNo = await _context.Set<WingEntity>()
                .Where(w => w.Id == society.WingId && w.IsActive)
                .Select(w => w.WingNo)
                .FirstOrDefaultAsync(cancellationToken);
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
        
        // Step 4: Upsert PropertyMastDetails (assessment) - includes NoOfToilets fields only
        // Note: WingNo is NOT stored in PropertyMastDetails, it's stored in SocietyDetailsMast
        var assessmentId = await _context.PropertyMastDetails
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasAssessmentData = dto.NoOfResidentialToilets.HasValue || dto.NoOfCommercialToilets.HasValue;

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
            OldCarpetAreaSqFeet = oldDetailsData?.OldCarpetAreaSqFeet,
            OldCarpetAreaSqMeter = oldDetailsData?.OldCarpetAreaSqMeter,
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
                                 dto.OldCarpetAreaSqMeter.HasValue ||
                                 dto.OldConstructionTypeId.HasValue || dto.OldTypeOfUseId.HasValue;

        if (oldDetailsId > 0)
        {
            // UPDATE existing record
            var oldDetailsData = await _context.PropertyDetailsOld.FindAsync(new object[] { oldDetailsId }, cancellationToken);

            if (oldDetailsData != null)
            {
                if (dto.OldConstructionYear != null)
                    oldDetailsData.OldConstructionYear = dto.OldConstructionYear;

                if (dto.OldCarpetAreaSqFeet.HasValue)
                    oldDetailsData.OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet;

                if (dto.OldCarpetAreaSqMeter.HasValue)
                    oldDetailsData.OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter;

                if (dto.OldConstructionTypeId.HasValue)
                    oldDetailsData.OldConstructionTypeId = dto.OldConstructionTypeId;

                if (dto.OldTypeOfUseId.HasValue)
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
                OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet,
                OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter,
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
public async Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var policies = await GetTaxDetailsPivotedAsync(
            propertyId,
            isCapitalValue: false,
            cancellationToken);

        if (policies == null)
            return null;

        return new PropertyTaxDetailsDto
        {
            PropertyId = propertyId,
            Policies = policies
        };
    }

    /// <summary>
    /// Private helper method to query and pivot tax details from a given tax details table.
    /// Joins with TaxMaster, filters by active/deleted flags, orders by DisplayOrder, and groups by PolicyCode.
    /// </summary>
    /// <typeparam name="TTaxDetail">The tax detail entity type (PolicyTaxDetails or PolicyTaxDetailsCV)</typeparam>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="taxDetailsSet">The DbSet of tax details to query</param>
    /// <param name="propertyIdSelector">Function to extract PropertyId from the entity</param>
    /// <param name="policyCodeSelector">Function to extract PolicyCode from the entity</param>
    /// <param name="taxIdSelector">Function to extract TaxId from the entity</param>
    /// <param name="taxAmountSelector">Function to extract TaxAmount from the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pivoted PolicyTaxDetail objects, or null if property not found or no data exists</returns>
    private async Task<List<PolicyTaxDetail>?> GetTaxDetailsPivotedAsync(
        int propertyId,
        bool isCapitalValue,
        CancellationToken cancellationToken)
    {
        // Step 1: Check if property exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Query tax details with TaxMaster join, ordered by DisplayOrder
        List<(string PolicyCode, string TaxName, decimal? TaxAmount)> taxData;

        if (isCapitalValue)
        {
            taxData = await (from td in _context.PolicyTaxDetailsCV
                             join tm in _context.TaxMaster on td.TaxId equals tm.Id
                             where td.PropertyId == propertyId && td.IsActive && !td.MarkedForDeletion
                                && tm.IsActive
                             orderby tm.DisplayOrder
                             select new ValueTuple<string, string, decimal?>(
                                 td.PolicyCode,
                                 tm.TaxName,
                                 td.TaxAmount
                             ))
                            .ToListAsync(cancellationToken);
        }
        else
        {
            taxData = await (from td in _context.PolicyTaxDetails
                             join tm in _context.TaxMaster on td.TaxId equals tm.Id
                             where td.PropertyId == propertyId && td.IsActive && !td.MarkedForDeletion
                                && tm.IsActive
                             orderby tm.DisplayOrder
                             select new ValueTuple<string, string, decimal?>(
                                 td.PolicyCode,
                                 tm.TaxName,
                                 td.TaxAmount
                             ))
                            .ToListAsync(cancellationToken);
        }

        // Step 3: Return null if no tax details found
        if (taxData.Count == 0)
            return null;

        // Step 4: Group by PolicyCode and create pivoted structure
        var policies = taxData
            .GroupBy(x => x.Item1)
            .Select(g => new PolicyTaxDetail
            {
                PolicyCode = g.Key,
                TaxAmounts = g
                    .GroupBy(x => x.Item2)
                    .ToDictionary(
                        tg => tg.Key,
                        tg => (decimal?)tg.Sum(x => x.Item3)
                    )
            })
            .ToList();

        return policies;
    }

    public async Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if property exists and is valid (active and not marked for deletion)
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Get all active old taxes from TaxMaster where OldTaxStatus = true
        var oldTaxes = await _context.TaxMaster
            .Where(t => t.IsActive && t.OldTaxStatus)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.TaxName, t.TaxNameAlias })
            .ToListAsync(cancellationToken);

        if (!oldTaxes.Any())
        {
            // Return empty result if no old taxes are configured
            return new PropertyOldTaxesDetailsDto
            {
                PropertyId = propertyId,
                TaxYears = new List<OldTaxYearDto>()
            };
        }

        // Step 3: Get all TransMastOld records for this property
        var transMastOldData = await _context.TransMastOld
            .Where(t => t.PropertyId == propertyId && t.IsActive && !t.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Step 4: Get unique finance years from the transactions
        var financeYearIds = transMastOldData.Select(t => t.FinanceYearId).Distinct().ToList();

        // Step 5: Get year details from YearMaster
        var years = await _context.YearMaster
            .Where(y => financeYearIds.Contains(y.Id) && y.IsActive)
            .OrderByDescending(y => y.Year)
            .Select(y => new { y.Id, y.Year, y.YearCode })
            .ToListAsync(cancellationToken);

        // Step 6: Build lookup dictionary for O(1) access (FinanceYearId, TaxId) -> Transaction
        var transactionLookup = transMastOldData
            .GroupBy(t => t.FinanceYearId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(t => t.TaxId, t => t)
            );

        // Step 7: Build the result
        var result = new PropertyOldTaxesDetailsDto
        {
            PropertyId = propertyId,
            TaxYears = new List<OldTaxYearDto>()
        };

        // Find the tax with name "Interest" (case-insensitive)
        var interestTaxId = oldTaxes.FirstOrDefault(t => 
            t.TaxName.Equals("Interest", StringComparison.OrdinalIgnoreCase))?.Id;

        foreach (var year in years)
        {
            // Get transactions for this year using O(1) lookup
            var hasYearTransactions = transactionLookup.TryGetValue(year.Id, out var yearTransactionsDict);

            var taxes = new List<TaxDetailDto>();
            decimal taxTotal = 0;
            decimal interest = 0;

            foreach (var tax in oldTaxes)
            {
                // O(1) lookup for transaction
                var taxAmount = hasYearTransactions && yearTransactionsDict!.TryGetValue(tax.Id, out var transaction)
                    ? transaction.TaxAmount
                    : 0;

                taxes.Add(new TaxDetailDto
                {
                    TaxId = tax.Id,
                    TaxName = tax.TaxNameAlias ?? tax.TaxName,
                    TaxAmount = taxAmount
                });

                // Check if this is the interest tax
                if (tax.Id == interestTaxId)
                {
                    interest = taxAmount;
                }
                else
                {
                    taxTotal += taxAmount;
                }
            }

            // Get RVorCV and RVorCVValue from first transaction for this year
            var firstTransaction = (hasYearTransactions && yearTransactionsDict != null && yearTransactionsDict.Any()) 
                ? yearTransactionsDict.Values.First() 
                : null;

            result.TaxYears.Add(new OldTaxYearDto
            {
                FinanceYearId = year.Id,
                Year = year.Year,
                YearCode = year.YearCode,
                RVorCV = firstTransaction?.RVorCV,
                RVorCVValue = firstTransaction?.RVorCVValue,
                Taxes = taxes,
                TaxTotal = taxTotal,
                Interest = interest,
                NetTotal = taxTotal + interest
            });
        }

        return result;
    }

    public async Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var policies = await GetTaxDetailsPivotedAsync(
            propertyId,
            isCapitalValue: true,
            cancellationToken);

        if (policies == null)
            return null;

        return new PropertyTaxDetailsCVDto
        {
            PropertyId = propertyId,
            Policies = policies
        };
    }

    public async Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
       var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;
    

        // Step 2: Validate finance years exist
        var requestedFinanceYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).ToList();
        var financeYearIds = requestedFinanceYearIds.Distinct().ToList();

        if (requestedFinanceYearIds.Count != financeYearIds.Count)
        {
            throw new InvalidOperationException("Duplicate finance years are not allowed in the request");
        }
        var validYearIds = await _context.YearMaster
            .Where(y => financeYearIds.Contains(y.Id) && y.IsActive)
            .Select(y => y.Id)
            .ToListAsync(cancellationToken);

        if (validYearIds.Count != financeYearIds.Count)
        {
            throw new InvalidOperationException("One or more finance years are invalid or inactive");
        }

        // Step 3: Validate all tax IDs exist in TaxMaster and have OldTaxStatus = true
        var allTaxIds = dto.TaxYears
            .SelectMany(ty => ty.Taxes.Select(t => t.TaxId))
            .Distinct()
            .ToList();

        var validTaxIds = await _context.TaxMaster
            .Where(t => allTaxIds.Contains(t.Id) && t.IsActive && t.OldTaxStatus)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (validTaxIds.Count != allTaxIds.Count)
        {
            throw new InvalidOperationException("One or more tax types are invalid, inactive, or not configured for old taxes");
        }

        // Step 4: Validate per-year uniqueness of TaxId to prevent duplicate inserts
        foreach (var yearDto in dto.TaxYears)
        {
            var duplicateTaxIds = yearDto.Taxes
                .GroupBy(t => t.TaxId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateTaxIds.Any())
            {
                throw new InvalidOperationException(
                    $"Duplicate TaxId(s) found for year {yearDto.FinanceYearId}: {string.Join(", ", duplicateTaxIds)}. " +
                    "Each tax can only appear once per finance year.");
            }

            // Validate and normalize RVorCV format
            var normalizedRVorCV = string.IsNullOrWhiteSpace(yearDto.RVorCV) ? "RV" : yearDto.RVorCV.Trim();
            if (normalizedRVorCV.Length > 2)
            {
                throw new InvalidOperationException(
                    $"RVorCV must be 2 characters or less for year {yearDto.FinanceYearId}. " +
                    $"Received: '{normalizedRVorCV}' ({normalizedRVorCV.Length} characters)");
            }
        }

        // Step 5: Prefetch all existing transactions for this property and requested years (fix N+1)
        // Do not filter by IsActive/MarkedForDeletion here because the database uniqueness
        // constraint is on (PropertyId, FinanceYearId, TaxId) regardless of soft-delete state.
        // If we exclude inactive or soft-deleted rows from the prefetch, later upsert logic can
        // miss an existing row and attempt to insert a duplicate key.
        var requestedYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).Distinct().ToList();
        var allExistingTransactions = await _context.TransMastOld
            .Where(t => t.PropertyId == propertyId &&
                       requestedYearIds.Contains(t.FinanceYearId))
            .ToListAsync(cancellationToken);

        // Build lookup dictionary for O(1) access: (FinanceYearId, TaxId) -> Transaction
        // Prefer the active/non-deleted row if more than one row is ever returned for the same key.
        var transactionLookup = allExistingTransactions
            .GroupBy(t => t.FinanceYearId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .GroupBy(t => t.TaxId)
                    .ToDictionary(
                        tg => tg.Key,
                        tg => tg
                            .OrderByDescending(t => t.IsActive && !t.MarkedForDeletion)
                            .First()
                    )
            );

        // Step 6: Process each year's tax data
        foreach (var yearDto in dto.TaxYears)
        {
            // Get normalized RVorCV (validated above)
            var normalizedRVorCV = string.IsNullOrWhiteSpace(yearDto.RVorCV) ? "RV" : yearDto.RVorCV.Trim();

            // Get existing transactions for this year using O(1) lookup
            var hasYearTransactions = transactionLookup.TryGetValue(yearDto.FinanceYearId, out var yearTransactionsDict);

            // Process each tax in the update DTO
            foreach (var taxDto in yearDto.Taxes)
            {
                // O(1) lookup for existing transaction
                var existingTransaction = hasYearTransactions && yearTransactionsDict!.TryGetValue(taxDto.TaxId, out var trans)
                    ? trans
                    : null;

                if (existingTransaction != null)
                {
                    // UPDATE existing transaction
                    existingTransaction.TaxAmount = taxDto.TaxAmount;
                    existingTransaction.RVorCV = normalizedRVorCV;
                    existingTransaction.RVorCVValue = yearDto.RVorCVValue ?? 0;
                    existingTransaction.IsActive = true;
                    existingTransaction.MarkedForDeletion = false;
                    existingTransaction.MarkedForDeletionDate = null;
                    existingTransaction.UpdatedDate = DateTime.Now;
                }
                else
                {
                    // INSERT new transaction
                    var newTransaction = new TransMastOldEntity
                    {
                        PropertyId = propertyId,
                        FinanceYearId = yearDto.FinanceYearId,
                        TaxId = taxDto.TaxId,
                        TaxAmount = taxDto.TaxAmount,
                        RVorCV = normalizedRVorCV,
                        RVorCVValue = yearDto.RVorCVValue ?? 0,
                        IsActive = true,
                        MarkedForDeletion = false,
                        CreatedDate = DateTime.Now
                    };

                    await _context.TransMastOld.AddAsync(newTransaction, cancellationToken);
                }
            }
        }

        // Step 7: Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        // Step 8: Return updated data
        return await GetOldTaxesDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Query PropertyDetailsOld with joins to master tables by ID
        var query = from pd in _context.PropertyDetailsOld
                    where pd.PropertyId == propertyId && pd.IsActive && !pd.MarkedForDeletion

                    join f in _context.FloorEntity on pd.OldFloorId equals f.Id into floorJoin
                    from f in floorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sf in _context.SubFloorEntity on pd.OldSubFloorId equals sf.Id into subFloorJoin
                    from sf in subFloorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join ct in _context.ConstructionTypeEntity on pd.OldConstructionTypeId equals ct.Id into constructionJoin
                    from ct in constructionJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join tu in _context.TypeOfUse on pd.OldTypeOfUseId equals tu.Id into typeOfUseJoin
                    from tu in typeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join stu in _context.SubTypeOfUse on pd.OldSubTypeOfUseId equals stu.Id into subTypeOfUseJoin
                    from stu in subTypeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    orderby pd.Id

                    select new
                    {
                        Id = pd.Id,
                        PropertyId = pd.PropertyId,
                        OldFloorId = pd.OldFloorId,
                        FloorDescription = f != null ? f.Description : null,
                        OldSubFloorId = pd.OldSubFloorId,
                        SubFloorDescription = sf != null ? sf.Description : null,
                        OldConstructionYear = pd.OldConstructionYear,
                        OldAssessmentYear = pd.OldAssessmentYear,
                        OldConstructionTypeId = pd.OldConstructionTypeId,
                        ConstructionTypeDescription = ct != null ? ct.Description : null,
                        OldTypeOfUseId = pd.OldTypeOfUseId,
                        TypeOfUseDescription = tu != null ? tu.Description : null,
                        OldSubTypeOfUseId = pd.OldSubTypeOfUseId,
                        SubTypeOfUseDescription = stu != null ? stu.Description : null,
                        OldCarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                        OldCarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                        OldBuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                        OldBuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                        MarkedForDeletion = pd.MarkedForDeletion,
                        MarkedForDeletionDate = pd.MarkedForDeletionDate
                    };

        var queryResults = await query.ToListAsync(cancellationToken);

        // Parse years in memory (cannot use TryParse in LINQ to Entities)
        var floorDetails = queryResults.Select(x => new PropertyDetailsOldDto
        {
            Id = x.Id,
            PropertyId = x.PropertyId,
            OldFloorId = x.OldFloorId,
            FloorDescription = x.FloorDescription,
            OldSubFloorId = x.OldSubFloorId,
            SubFloorDescription = x.SubFloorDescription,
            OldConstructionYear = x.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(x.OldConstructionYear) && int.TryParse(x.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = x.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(x.OldAssessmentYear) && int.TryParse(x.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = x.OldConstructionTypeId,
            ConstructionTypeDescription = x.ConstructionTypeDescription,
            OldTypeOfUseId = x.OldTypeOfUseId,
            TypeOfUseDescription = x.TypeOfUseDescription,
            OldSubTypeOfUseId = x.OldSubTypeOfUseId,
            SubTypeOfUseDescription = x.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = x.OldCarpetAreaSqMeter,
            OldCarpetAreaSqFeet = x.OldCarpetAreaSqFeet,
            OldBuiltupAreaSqMeter = x.OldBuiltupAreaSqMeter,
            OldBuiltupAreaSqFeet = x.OldBuiltupAreaSqFeet,
            MarkedForDeletion = x.MarkedForDeletion,
            MarkedForDeletionDate = x.MarkedForDeletionDate
        }).ToList();

        return new PropertyDetailsOldListDto
        {
            PropertyId = propertyId,
            FloorDetails = floorDetails
        };
    }

    public async Task<PropertyDetailsOldListDto?> UpdateFloorDetailsOldAsync(int propertyId, UpdatePropertyDetailsOldListDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Check if PropertyMast exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Get all existing records for this property
        var existingRecords = await _context.PropertyDetailsOld
            .Where(pd => pd.PropertyId == propertyId && pd.IsActive && !pd.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var existingRecordsDict = existingRecords.ToDictionary(r => r.Id);

        // Step 3: Collect all IDs from the incoming request (only non-null, positive IDs)
        var incomingIds = dto.FloorDetails
            .Where(r => r.Id.HasValue && r.Id.Value > 0)
            .Select(r => r.Id!.Value)
            .ToHashSet();

        // Step 4: Validate all foreign keys upfront (for both new and update records)
        var allFloorIds = dto.FloorDetails
            .Where(r => r.OldFloorId.HasValue)
            .Select(r => r.OldFloorId!.Value)
            .Distinct()
            .ToList();

        if (allFloorIds.Any())
        {
            var validFloorIds = await _context.FloorEntity
                .Where(f => allFloorIds.Contains(f.Id) && f.IsActive)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            var invalidFloorIds = allFloorIds.Except(validFloorIds).ToList();
            if (invalidFloorIds.Any())
            {
                throw new InvalidOperationException($"Invalid or inactive Floor ID(s): {string.Join(", ", invalidFloorIds)}");
            }
        }

        var allConstructionTypeIds = dto.FloorDetails
            .Where(r => r.OldConstructionTypeId.HasValue)
            .Select(r => r.OldConstructionTypeId!.Value)
            .Distinct()
            .ToList();

        if (allConstructionTypeIds.Any())
        {
            var validConstructionTypeIds = await _context.ConstructionTypeEntity
                .Where(c => allConstructionTypeIds.Contains(c.Id) && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var invalidConstructionTypeIds = allConstructionTypeIds.Except(validConstructionTypeIds).ToList();
            if (invalidConstructionTypeIds.Any())
            {
                throw new InvalidOperationException($"Invalid or inactive ConstructionType ID(s): {string.Join(", ", invalidConstructionTypeIds)}");
            }
        }

        var allTypeOfUseIds = dto.FloorDetails
            .Where(r => r.OldTypeOfUseId.HasValue)
            .Select(r => r.OldTypeOfUseId!.Value)
            .Distinct()
            .ToList();

        if (allTypeOfUseIds.Any())
        {
            var validTypeOfUseIds = await _context.TypeOfUse
                .Where(t => allTypeOfUseIds.Contains(t.Id) && t.IsActive)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            var invalidTypeOfUseIds = allTypeOfUseIds.Except(validTypeOfUseIds).ToList();
            if (invalidTypeOfUseIds.Any())
            {
                throw new InvalidOperationException($"Invalid or inactive TypeOfUse ID(s): {string.Join(", ", invalidTypeOfUseIds)}");
            }
        }

        var allSubFloorIds = dto.FloorDetails
            .Where(r => r.OldSubFloorId.HasValue)
            .Select(r => r.OldSubFloorId!.Value)
            .Distinct()
            .ToList();

        if (allSubFloorIds.Any())
        {
            var validSubFloorIds = await _context.SubFloorEntity
                .Where(sf => allSubFloorIds.Contains(sf.Id) && sf.IsActive)
                .Select(sf => sf.Id)
                .ToListAsync(cancellationToken);

            var invalidSubFloorIds = allSubFloorIds.Except(validSubFloorIds).ToList();
            if (invalidSubFloorIds.Any())
            {
                throw new InvalidOperationException($"Invalid or inactive SubFloor ID(s): {string.Join(", ", invalidSubFloorIds)}");
            }
        }

        var allSubTypeOfUseIds = dto.FloorDetails
            .Where(r => r.OldSubTypeOfUseId.HasValue)
            .Select(r => r.OldSubTypeOfUseId!.Value)
            .Distinct()
            .ToList();

        if (allSubTypeOfUseIds.Any())
        {
            var validSubTypeOfUseIds = await _context.SubTypeOfUse
                .Where(stu => allSubTypeOfUseIds.Contains(stu.Id) && stu.IsActive)
                .Select(stu => stu.Id)
                .ToListAsync(cancellationToken);

            var invalidSubTypeOfUseIds = allSubTypeOfUseIds.Except(validSubTypeOfUseIds).ToList();
            if (invalidSubTypeOfUseIds.Any())
            {
                throw new InvalidOperationException($"Invalid or inactive SubTypeOfUse ID(s): {string.Join(", ", invalidSubTypeOfUseIds)}");
            }
        }

        // Step 5: Process each record - UPSERT logic based on Id
        foreach (var record in dto.FloorDetails)
        {
            if (record.Id.HasValue && record.Id.Value > 0)
            {
                // UPDATE existing record
                if (!existingRecordsDict.TryGetValue(record.Id.Value, out var entity))
                {
                    throw new InvalidOperationException($"PropertyDetailsOld record with ID {record.Id.Value} not found or does not belong to property {propertyId}");
                }

                entity.OldFloorId = record.OldFloorId;
                entity.OldSubFloorId = record.OldSubFloorId;
                entity.OldConstructionYear = record.OldConstructionYear;
                entity.OldAssessmentYear = record.OldAssessmentYear;
                entity.OldConstructionTypeId = record.OldConstructionTypeId;
                entity.OldTypeOfUseId = record.OldTypeOfUseId;
                entity.OldSubTypeOfUseId = record.OldSubTypeOfUseId;
                entity.OldCarpetAreaSqMeter = record.OldCarpetAreaSqMeter;
                entity.OldCarpetAreaSqFeet = record.OldCarpetAreaSqFeet;
                entity.OldBuiltupAreaSqMeter = record.OldBuiltupAreaSqMeter;
                entity.OldBuiltupAreaSqFeet = record.OldBuiltupAreaSqFeet;
                entity.UpdatedDate = DateTime.Now;
            }
            else
            {
                // INSERT new record (Id will be auto-generated by database)
                var newEntity = new PropertyDetailsOldEntity
                {
                    PropertyId = propertyId,
                    OldFloorId = record.OldFloorId,
                    OldSubFloorId = record.OldSubFloorId,
                    OldConstructionYear = record.OldConstructionYear,
                    OldAssessmentYear = record.OldAssessmentYear,
                    OldConstructionTypeId = record.OldConstructionTypeId,
                    OldTypeOfUseId = record.OldTypeOfUseId,
                    OldSubTypeOfUseId = record.OldSubTypeOfUseId,
                    OldCarpetAreaSqMeter = record.OldCarpetAreaSqMeter,
                    OldCarpetAreaSqFeet = record.OldCarpetAreaSqFeet,
                    OldBuiltupAreaSqMeter = record.OldBuiltupAreaSqMeter,
                    OldBuiltupAreaSqFeet = record.OldBuiltupAreaSqFeet,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = DateTime.Now
                };

                await _context.PropertyDetailsOld.AddAsync(newEntity, cancellationToken);
            }
        }

        // Step 6: Soft DELETE records not in the incoming list (records that were removed from UI)
        var recordsToDelete = existingRecords.Where(r => !incomingIds.Contains(r.Id)).ToList();
        foreach (var record in recordsToDelete)
        {
            record.MarkedForDeletion = true;
            record.IsActive = false;
            record.MarkedForDeletionDate = DateTime.Now;
            record.UpdatedDate = DateTime.Now;
        }

        // Step 7: Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        // Step 8: Return updated data
        return await GetFloorDetailsOldAsync(propertyId, cancellationToken);
    }
}
 
