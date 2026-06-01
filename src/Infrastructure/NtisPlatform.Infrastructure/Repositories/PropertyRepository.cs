using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Application.Enums;

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

        // Step 3: Update PropertyMast fields (always update, even if null)
        property.WardId = dto.WardId;
        property.TaxZoneId = dto.TaxZoneId;
        property.CategoryId = dto.CategoryId;
        property.PropertyTypeId = dto.PropertyTypeId;
        property.PartitionNo = dto.PartitionNo;
        property.FlatOrShopNo = dto.FlatOrShopNo;
        property.PlotNo = dto.PlotNo;
        property.CSN = dto.SurveyNo;
        property.UPICId = dto.UPICId;
        property.SubZoneNo = dto.SubZoneNo;
        property.MoujaId = dto.MoujaId;
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
                // Always update, even if null
                assessment.NoOfResidentialToilets = dto.NoOfResidentialToilets;
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
                // Always update, even if null
                plot.PlotArea = dto.PlotArea;
                plot.PlotAreaFtLength = dto.PlotAreaFtLength;
                plot.PlotAreaFtWidth = dto.PlotAreaFtWidth;
                plot.PlotAreaMtrLength = dto.PlotAreaMtrLength;
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

        // Step 6: Upsert SocietyDetailsMast for WingId, WingNo, and WingName
        // Always process society details if any wing-related data is sent (including null to clear)
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

        // Create new society if still not found and any wing data is being set
        if (society == null && (dto.WingId.HasValue || dto.WingName != null || dto.WingNo != null))
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

        // Update society fields if society exists
        // NOTE: This always updates fields, even if null, to allow clearing of wing data.
        // If a client sends null for WingId/WingName, those fields will be cleared in the database.
        // This is intentional behavior per the requirement that null values should update fields to NULL.
        if (society != null)
        {
            society.WingId = dto.WingId;
            society.WingName = dto.WingName;

            // If WingNo is provided, try to find matching WingEntity
            if (dto.WingNo != null)
            {
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

        // Step 4: Update society details fields (always update, even if null)
        society.WingId = dto.WingId;
        society.WingName = dto.WingName;
        society.SocietyName = dto.SocietyName;
        society.SocietyAddress = dto.SocietyAddress;
        society.SecretaryName = dto.SecretaryName;
        society.ManagerName = dto.ManagerName;
        society.LandOwnerName = dto.LandOwnerName;
        society.BuilderName = dto.BuilderName;
        society.SocietyNameEnglish = dto.SocietyNameEnglish;
        society.SocietyAddressEnglish = dto.SocietyAddressEnglish;
        society.SecretaryNameEnglish = dto.SecretaryNameEnglish;
        society.ManagerNameEnglish = dto.ManagerNameEnglish;
        society.LandOwnerNameEnglish = dto.LandOwnerNameEnglish;
        society.BuilderNameEnglish = dto.BuilderNameEnglish;
        society.ManagerMobileNo = dto.ManagerMobileNo;
        society.SecretaryMobileNo = dto.SecretaryMobileNo;
        society.SocietyEmailId = dto.SocietyEmailId;
        society.SecretaryEmailId = dto.SecretaryEmailId;
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

        // Step 2: Update PropertyMast fields (always update, even if null)
        property.OwnerTitle = dto.OwnerTitle;
        property.OwnerName = dto.OwnerName;
        property.OwnerTitleEnglish = dto.OwnerTitleEnglish;
        property.OwnerNameEnglish = dto.OwnerNameEnglish;
        property.OccupierTitle = dto.OccupierTitle;
        property.OccupierName = dto.OccupierName;
        property.OccupierTitleEnglish = dto.OccupierTitleEnglish;
        property.OccupierNameEnglish = dto.OccupierNameEnglish;
        property.Address = dto.Address;
        property.Location = dto.Location;
        property.AddressEnglish = dto.AddressEnglish;
        property.LocationEnglish = dto.LocationEnglish;
        property.FlatOrShopName = dto.FlatOrShopName;
        property.FlatOrShopNameEnglish = dto.FlatOrShopNameEnglish;
        property.FlatOrShopNo = dto.FlatOrShopNo;
        property.FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish;
        property.MobileNo = dto.MobileNo;
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
                // Always update, even if null
                assessment.OwnerTypeId = dto.OwnerTypeId;
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
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
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
            .Where(x => x.Id == propertyMastOldId && x.IsActive && !x.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Get first PropertyDetailsOld data (or aggregate if needed)
        var oldDetailsData = await _context.PropertyDetailsOld
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

    public async Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Get or create PropertyMastOld for this property
            var property = await _context.PropertyMast
                .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
                .Select(p => new { p.Id, p.PropertyMastOldId })
                .FirstOrDefaultAsync(cancellationToken);

            if (property == null)
                return null;

            int propertyMastOldId;

            // Step 2: Check if PropertyMastOld exists or create it
            if (property.PropertyMastOldId.HasValue)
            {
                propertyMastOldId = property.PropertyMastOldId.Value;
            }
            else
            {
                // Create new PropertyMastOld record
                var newPropertyMastOld = new PropertyMastOldEntity
                {
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = DateTime.Now
                };
                await _context.PropertyMastOld.AddAsync(newPropertyMastOld, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                propertyMastOldId = newPropertyMastOld.Id;

                // Update PropertyMast with the new PropertyMastOldId
                var propertyEntity = await _context.PropertyMast.FindAsync(new object[] { propertyId }, cancellationToken);
                if (propertyEntity != null)
                {
                    propertyEntity.PropertyMastOldId = propertyMastOldId;
                    propertyEntity.UpdatedDate = DateTime.Now;
                }
            }

            // Step 3: Update PropertyMastOld fields
            var oldMastData = await _context.PropertyMastOld.FindAsync(new object[] { propertyMastOldId }, cancellationToken);

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

                if (dto.OldConstructionArea != null)
                    oldMastData.OldConstructionArea = dto.OldConstructionArea;

                if (dto.OldGeneralTax != null)
                    oldMastData.OldGeneralTax = dto.OldGeneralTax;

                if (dto.OldCSN != null)
                    oldMastData.OldCSN = dto.OldCSN;

                oldMastData.UpdatedDate = DateTime.Now;
            }

            // Step 4: Upsert PropertyDetailsOld
            var oldDetailsId = await _context.PropertyDetailsOld
                .Where(x => x.PropertyMastOldId == propertyMastOldId && x.IsActive && !x.MarkedForDeletion)
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
                        oldDetailsData.OldConstructionTypeId = dto.OldConstructionTypeId.Value;

                    if (dto.OldTypeOfUseId.HasValue)
                        oldDetailsData.OldTypeOfUseId = dto.OldTypeOfUseId.Value;

                    if (dto.OldFloorId.HasValue)
                        oldDetailsData.OldFloorId = dto.OldFloorId.Value;

                    oldDetailsData.UpdatedDate = DateTime.Now;
                }
            }
            else if (hasOldDetailsData)
            {
                // Validate required fields before insert
                if (!dto.OldFloorId.HasValue)
                    throw new InvalidOperationException("OldFloorId is required.");
                if (!dto.OldConstructionTypeId.HasValue)
                    throw new InvalidOperationException("OldConstructionTypeId is required.");
                if (!dto.OldTypeOfUseId.HasValue)
                    throw new InvalidOperationException("OldTypeOfUseId is required.");

                // INSERT new record only if data is provided
                var newOldDetailsData = new PropertyDetailsOldEntity
                {
                    PropertyMastOldId = propertyMastOldId,
                    OldConstructionYear = dto.OldConstructionYear,
                    OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet,
                    OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter,
                    OldConstructionTypeId = dto.OldConstructionTypeId.Value,
                    OldTypeOfUseId = dto.OldTypeOfUseId.Value,
                    OldFloorId = dto.OldFloorId.Value,
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
        catch (InvalidOperationException)
        {
            // Re-throw validation exceptions as-is (will be handled as 400 by controller)
            throw;
        }
        catch (ArgumentException)
        {
            // Re-throw validation exceptions as-is (will be handled as 400 by controller)
            throw;
        }
        catch (Exception ex)
        {
            // Wrap unexpected errors only
            throw new Exception($"An error occurred while updating old property details for Property ID: {propertyId}. Internal Error: {ex.Message}", ex);
        }
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
            .Select(g =>
            {
                var taxAmounts = g
                    .GroupBy(x => x.Item2)
                    .Select(tg => new TaxAmountDetail
                    {
                        TaxName = tg.Key,
                        TaxAmount = tg.Sum(x => x.Item3 ?? 0)
                    })
                    .ToList();

                var taxTotal = taxAmounts.Sum(t => t.TaxAmount);

                return new PolicyTaxDetail
                {
                    PolicyCode = g.Key,
                    TaxAmounts = taxAmounts,
                    TaxTotal = taxTotal
                };
            })
            .ToList();

        return policies;
    }

    public async Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
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

        // Step 3: Check if PropertyMastOld exists
        if (!property.PropertyMastOldId.HasValue)
        {
            // No PropertyMastOld linked - return empty list with all taxes available for insert
            // UI can add new years from YearMaster dropdown
            return new PropertyOldTaxesDetailsDto
            {
                PropertyId = propertyId,
                TaxYears = new List<OldTaxYearDto>()
            };
        }

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 4: Get all TransMastOld records for this PropertyMastOldId
        var transMastOldData = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == propertyMastOldId && t.IsActive && !t.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Step 5: Get unique finance years from the transactions
        var financeYearIds = transMastOldData.Select(t => t.FinanceYearId).Distinct().ToList();

        // Step 6: Check if there are any existing transactions
        if (!financeYearIds.Any())
        {
            // PropertyMastOld exists but no TransMastOld records
            // Return empty list with all taxes available for insert
            return new PropertyOldTaxesDetailsDto
            {
                PropertyId = propertyId,
                TaxYears = new List<OldTaxYearDto>()
            };
        }

        // Step 7: Get year details from YearMaster
        var years = await _context.YearMaster
            .Where(y => financeYearIds.Contains(y.Id) && y.IsActive)
            .OrderByDescending(y => y.Year)
            .Select(y => new { y.Id, y.Year, y.YearCode })
            .ToListAsync(cancellationToken);

        // Step 8: Build lookup dictionary for O(1) access (FinanceYearId, TaxId) -> Transaction
        var transactionLookup = transMastOldData
            .GroupBy(t => t.FinanceYearId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(t => t.TaxId, t => t)
            );

        // Step 9: Build the result
        var result = new PropertyOldTaxesDetailsDto
        {
            PropertyId = propertyId,
            TaxYears = new List<OldTaxYearDto>()
        };

        // Find the tax with name "Interest" (case-insensitive) or alias "Interest"
        var interestTaxId = oldTaxes.FirstOrDefault(t =>
            t.TaxName.Equals("Interest", StringComparison.OrdinalIgnoreCase) ||
            (t.TaxNameAlias != null && t.TaxNameAlias.Equals("Interest", StringComparison.OrdinalIgnoreCase)))?.Id;

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

    public async Task<PropertyOldTaxesDetailsDto?> CreateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Verify property exists (no changes yet)
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Validate finance years exist (no changes yet)
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

        // Step 3: Validate all tax IDs exist in TaxMaster and have OldTaxStatus = true (no changes yet)
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

        // Step 4: Validate per-year uniqueness of TaxId (no changes yet)
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

        // Step 5: Check for existing active transactions IF PropertyMastOld exists (no changes yet)
        if (property.PropertyMastOldId.HasValue)
        {
            var requestedYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).Distinct().ToList();
            var requestedYearTaxCombinations = dto.TaxYears
                .SelectMany(ty => ty.Taxes.Select(t => new { YearId = ty.FinanceYearId, TaxId = t.TaxId }))
                .ToList();

            var existingActiveTransactions = await _context.TransMastOld
                .Where(t => t.PropertyMastOldId == property.PropertyMastOldId.Value &&
                           requestedYearIds.Contains(t.FinanceYearId) &&
                           t.IsActive &&
                           !t.MarkedForDeletion)
                .Select(t => new { t.FinanceYearId, t.TaxId })
                .ToListAsync(cancellationToken);

            var conflicts = requestedYearTaxCombinations
                .Where(req => existingActiveTransactions.Any(exist => 
                    exist.FinanceYearId == req.YearId && exist.TaxId == req.TaxId))
                .ToList();

            if (conflicts.Any())
            {
                // Find which year-tax combinations already exist

                var yearNames = await _context.YearMaster
                    .Where(y => conflicts.Select(c => c.YearId).Distinct().Contains(y.Id))
                    .Select(y => new { y.Id, y.YearCode })
                    .ToListAsync(cancellationToken);

                var taxNames = await _context.TaxMaster
                    .Where(t => conflicts.Select(c => c.TaxId).Distinct().Contains(t.Id))
                    .Select(t => new { t.Id, t.TaxName })
                    .ToListAsync(cancellationToken);

                var conflictDetails = conflicts
                    .Select(c => 
                    {
                        var year = yearNames.FirstOrDefault(y => y.Id == c.YearId)?.YearCode ?? c.YearId.ToString();
                        var tax = taxNames.FirstOrDefault(t => t.Id == c.TaxId)?.TaxName ?? c.TaxId.ToString();
                        return $"{year} - {tax}";
                    })
                    .ToList();

                throw new InvalidOperationException(
                    $"Cannot create records - the following year-tax combinations already exist: {string.Join(", ", conflictDetails)}. " +
                    "Use PUT endpoint to update existing records.");
            }
        }

        // ALL VALIDATIONS PASSED - Now begin transaction for atomic database changes
        // Use execution strategy for resilience
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Try to begin a transaction (will be ignored by InMemory provider in tests)
            using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                int propertyMastOldId;

                // Step 6: Create PropertyMastOld if needed (inside transaction)
                if (property.PropertyMastOldId.HasValue)
                {
                    propertyMastOldId = property.PropertyMastOldId.Value;
                }
                else
                {
                    // Create new PropertyMastOld record
                    var newPropertyMastOld = new PropertyMastOldEntity
                    {
                        IsActive = true,
                        MarkedForDeletion = false,
                        CreatedDate = DateTime.Now
                    };
                    await _context.PropertyMastOld.AddAsync(newPropertyMastOld, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    propertyMastOldId = newPropertyMastOld.Id;

                    // Update PropertyMast with the new PropertyMastOldId
                    var propertyEntity = await _context.PropertyMast.FindAsync(new object[] { propertyId }, cancellationToken);
                    if (propertyEntity != null)
                    {
                        propertyEntity.PropertyMastOldId = propertyMastOldId;
                        propertyEntity.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                // Step 7: Insert new transactions only (Create-Only operation)
                foreach (var yearDto in dto.TaxYears)
                {
                    // Get normalized RVorCV (validated above)
                    var normalizedRVorCV = string.IsNullOrWhiteSpace(yearDto.RVorCV) ? "RV" : yearDto.RVorCV.Trim();

                    // Process each tax in the create DTO
                    foreach (var taxDto in yearDto.Taxes)
                    {
                        // INSERT new transaction
                        var newTransaction = new TransMastOldEntity
                        {
                            PropertyMastOldId = propertyMastOldId,
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

                // Persist newly added TransMastOld rows so the totals reflect the current request
                await _context.SaveChangesAsync(cancellationToken);

                // Step 8: Update PropertyMastOld.OldTotalTax and OldGeneralTax with sum from TransMastOld
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

                // Calculate General Tax
                double? generalTaxFromTransMastOld = null;
                if (generalTaxId.HasValue)
                {
                    generalTaxFromTransMastOld = await _context.TransMastOld
                        .Where(t => t.PropertyMastOldId == propertyMastOldId && 
                                   t.IsActive && 
                                   !t.MarkedForDeletion &&
                                   t.TaxId == generalTaxId.Value)
                        .SumAsync(t => (double?)t.TaxAmount, cancellationToken);
                }

                var propertyMastOldEntity = await _context.PropertyMastOld.FindAsync(new object[] { propertyMastOldId }, cancellationToken);
                if (propertyMastOldEntity != null)
                {
                    propertyMastOldEntity.OldTotalTax = totalTaxFromTransMastOld;
                    if (generalTaxId.HasValue)
                    {
                        propertyMastOldEntity.OldGeneralTax = generalTaxFromTransMastOld;
                    }
                    propertyMastOldEntity.UpdatedDate = DateTime.Now;
                }

                // Step 9: Save all changes
                await _context.SaveChangesAsync(cancellationToken);

                // Commit transaction if we created one
                transaction?.Commit();

                // Step 10: Return created data
                return await GetOldTaxesDetailsAsync(propertyId, cancellationToken);
            }
            catch
            {
                // Rollback transaction on any error (if transaction exists)
                transaction?.Rollback();
                throw;
            }
        });
    }

    public async Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Get or create PropertyMastOld for this property
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;


        int propertyMastOldId;

        // Step 2: Check if PropertyMastOld exists or create it
        if (property.PropertyMastOldId.HasValue)
        {
            propertyMastOldId = property.PropertyMastOldId.Value;
        }
        else
        {
            // Create new PropertyMastOld record
            var newPropertyMastOld = new PropertyMastOldEntity
            {
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            await _context.PropertyMastOld.AddAsync(newPropertyMastOld, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            propertyMastOldId = newPropertyMastOld.Id;

            // Update PropertyMast with the new PropertyMastOldId
            var propertyEntity = await _context.PropertyMast.FindAsync(new object[] { propertyId }, cancellationToken);
            if (propertyEntity != null)
            {
                propertyEntity.PropertyMastOldId = propertyMastOldId;
                propertyEntity.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

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

        // Step 5: Prefetch all existing transactions for this PropertyMastOldId and requested years (fix N+1)
        // Do not filter by IsActive/MarkedForDeletion here even though the database has a filtered unique index
        // (PropertyMastOldId, FinanceYearId, TaxId WHERE IsActive=1 AND MarkedForDeletion=0).
        // We need to load all rows to support reactivation: if a soft-deleted row exists,
        // the upsert logic must UPDATE (not INSERT) to avoid constraint violations when reactivating.
        var requestedYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).Distinct().ToList();
        var allExistingTransactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == propertyMastOldId &&
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
        // All taxes (including Interest if configured) are now treated equally
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
                        PropertyMastOldId = propertyMastOldId,
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

        // Persist transaction changes so the totals are computed from the latest values
        await _context.SaveChangesAsync(cancellationToken);

        // Step 7: Update PropertyMastOld.OldTotalTax and OldGeneralTax with sum from TransMastOld
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

        // Calculate General Tax
        double? generalTaxFromTransMastOld = null;
        if (generalTaxId.HasValue)
        {
            generalTaxFromTransMastOld = await _context.TransMastOld
                .Where(t => t.PropertyMastOldId == propertyMastOldId && 
                           t.IsActive && 
                           !t.MarkedForDeletion &&
                           t.TaxId == generalTaxId.Value)
                .SumAsync(t => (double?)t.TaxAmount, cancellationToken);
        }

        var propertyMastOldEntity = await _context.PropertyMastOld.FindAsync(new object[] { propertyMastOldId }, cancellationToken);
        if (propertyMastOldEntity != null)
        {
            propertyMastOldEntity.OldTotalTax = totalTaxFromTransMastOld;
            if (generalTaxId.HasValue)
            {
                propertyMastOldEntity.OldGeneralTax = generalTaxFromTransMastOld;
            }
            propertyMastOldEntity.UpdatedDate = DateTime.Now;
        }

        // Step 8: Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        // Step 9: Return updated data
        return await GetOldTaxesDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
            return new PropertyDetailsOldListDto { PropertyId = propertyId, FloorDetails = new List<PropertyDetailsOldDto>() };

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Query PropertyDetailsOld with joins to master tables by ID
        var query = from pd in _context.PropertyDetailsOld
                    where pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion

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
            PropertyId = propertyId,
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
            OldCarpetAreaSqMeter = x.OldCarpetAreaSqMeter.HasValue ? Math.Round(x.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = x.OldCarpetAreaSqFeet.HasValue ? Math.Round(x.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = x.OldBuiltupAreaSqMeter.HasValue ? Math.Round(x.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = x.OldBuiltupAreaSqFeet.HasValue ? Math.Round(x.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = x.MarkedForDeletion,
            MarkedForDeletionDate = x.MarkedForDeletionDate
        }).ToList();

        return new PropertyDetailsOldListDto
        {
            PropertyId = propertyId,
            FloorDetails = floorDetails
        };
    }

    public async Task<FloorDetailsOldPagedResult?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQuery query, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
        {
            // Normalize metadata for empty result set with unpaged mode
            var emptyPageSize = query.PageSize == -1 ? 1 : query.PageSize;
            var emptyPageNumber = query.PageSize == -1 ? 1 : query.PageNumber;
            return new FloorDetailsOldPagedResult 
            { 
                TotalCount = 0, 
                PageNumber = emptyPageNumber, 
                PageSize = emptyPageSize, 
                Items = new List<PropertyDetailsOldDto>() 
            };
        }

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Build base query with joins to master tables by ID
        var baseQuery = from pd in _context.PropertyDetailsOld
                    where pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion

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

                    select new
                    {
                        Id = pd.Id,
                        PropertyId = propertyId,
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

        // Step 3: Apply filters
        if (query.OldFloorId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldFloorId == query.OldFloorId.Value);

        if (query.OldSubFloorId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldSubFloorId == query.OldSubFloorId.Value);

        if (query.OldConstructionTypeId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldConstructionTypeId == query.OldConstructionTypeId.Value);

        if (query.OldTypeOfUseId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldTypeOfUseId == query.OldTypeOfUseId.Value);

        if (query.OldSubTypeOfUseId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldSubTypeOfUseId == query.OldSubTypeOfUseId.Value);

        if (!string.IsNullOrWhiteSpace(query.OldConstructionYear))
            baseQuery = baseQuery.Where(x => x.OldConstructionYear == query.OldConstructionYear);

        if (!string.IsNullOrWhiteSpace(query.OldAssessmentYear))
            baseQuery = baseQuery.Where(x => x.OldAssessmentYear == query.OldAssessmentYear);

        // Step 4: Apply search term if provided
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(x =>
                (x.FloorDescription != null && x.FloorDescription.ToLower().Contains(searchTerm)) ||
                (x.SubFloorDescription != null && x.SubFloorDescription.ToLower().Contains(searchTerm)) ||
                (x.ConstructionTypeDescription != null && x.ConstructionTypeDescription.ToLower().Contains(searchTerm)) ||
                (x.TypeOfUseDescription != null && x.TypeOfUseDescription.ToLower().Contains(searchTerm)) ||
                (x.SubTypeOfUseDescription != null && x.SubTypeOfUseDescription.ToLower().Contains(searchTerm))
            );
        }

        // Step 5: Apply sorting
        var isDescending = query.SortOrder?.ToLower() == "desc";
        var sortBy = query.SortBy?.ToLower();

        baseQuery = sortBy switch
        {
            "id" => isDescending ? baseQuery.OrderByDescending(x => x.Id) : baseQuery.OrderBy(x => x.Id),
            "oldfloorid" => isDescending ? baseQuery.OrderByDescending(x => x.OldFloorId) : baseQuery.OrderBy(x => x.OldFloorId),
            "oldsubfloorid" => isDescending ? baseQuery.OrderByDescending(x => x.OldSubFloorId) : baseQuery.OrderBy(x => x.OldSubFloorId),
            "oldconstructiontypeid" => isDescending ? baseQuery.OrderByDescending(x => x.OldConstructionTypeId) : baseQuery.OrderBy(x => x.OldConstructionTypeId),
            "oldtypeofuseid" => isDescending ? baseQuery.OrderByDescending(x => x.OldTypeOfUseId) : baseQuery.OrderBy(x => x.OldTypeOfUseId),
            "oldsubtypeofuseid" => isDescending ? baseQuery.OrderByDescending(x => x.OldSubTypeOfUseId) : baseQuery.OrderBy(x => x.OldSubTypeOfUseId),
            "oldconstructionyear" => isDescending ? baseQuery.OrderByDescending(x => x.OldConstructionYear) : baseQuery.OrderBy(x => x.OldConstructionYear),
            "oldassessmentyear" => isDescending ? baseQuery.OrderByDescending(x => x.OldAssessmentYear) : baseQuery.OrderBy(x => x.OldAssessmentYear),
            _ => baseQuery.OrderBy(x => x.Id)
        };

        // Step 6: Get total count
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Step 7: Apply pagination
        // Handle unpaged mode (PageSize == -1): return all records with normalized metadata
        var returnAllRecords = query.PageSize == -1;
        var pagedQuery = returnAllRecords
            ? baseQuery
            : baseQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);

        var queryResults = await pagedQuery.ToListAsync(cancellationToken);

        // Step 8: Parse years in memory (cannot use TryParse in LINQ to Entities)
        var floorDetails = queryResults.Select(x => new PropertyDetailsOldDto
        {
            Id = x.Id,
            PropertyId = propertyId,
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
            OldCarpetAreaSqMeter = x.OldCarpetAreaSqMeter.HasValue ? Math.Round(x.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = x.OldCarpetAreaSqFeet.HasValue ? Math.Round(x.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = x.OldBuiltupAreaSqMeter.HasValue ? Math.Round(x.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = x.OldBuiltupAreaSqFeet.HasValue ? Math.Round(x.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = x.MarkedForDeletion,
            MarkedForDeletionDate = x.MarkedForDeletionDate
        }).ToList();

        // Normalize pagination metadata for unpaged mode to avoid division by zero in TotalPages calculation
        // When PageSize == -1, set PageNumber = 1 and PageSize = max(1, totalCount) to ensure valid metadata
        var normalizedPageNumber = returnAllRecords ? 1 : query.PageNumber;
        var normalizedPageSize = returnAllRecords ? Math.Max(1, totalCount) : query.PageSize;

        return new FloorDetailsOldPagedResult
        {
            Items = floorDetails,
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
    }

    public async Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
            return null;

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Query single PropertyDetailsOld record with joins
        var query = from pd in _context.PropertyDetailsOld
                    where pd.Id == floorId && pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion

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

                    select new
                    {
                        Id = pd.Id,
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

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new PropertyDetailsOldDto
        {
            Id = result.Id,
            PropertyId = propertyId,
            OldFloorId = result.OldFloorId,
            FloorDescription = result.FloorDescription,
            OldSubFloorId = result.OldSubFloorId,
            SubFloorDescription = result.SubFloorDescription,
            OldConstructionYear = result.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(result.OldConstructionYear) && int.TryParse(result.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = result.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(result.OldAssessmentYear) && int.TryParse(result.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = result.OldConstructionTypeId,
            ConstructionTypeDescription = result.ConstructionTypeDescription,
            OldTypeOfUseId = result.OldTypeOfUseId,
            TypeOfUseDescription = result.TypeOfUseDescription,
            OldSubTypeOfUseId = result.OldSubTypeOfUseId,
            SubTypeOfUseDescription = result.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = result.OldCarpetAreaSqMeter.HasValue ? Math.Round(result.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = result.OldCarpetAreaSqFeet.HasValue ? Math.Round(result.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = result.OldBuiltupAreaSqMeter.HasValue ? Math.Round(result.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = result.OldBuiltupAreaSqFeet.HasValue ? Math.Round(result.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = result.MarkedForDeletion,
            MarkedForDeletionDate = result.MarkedForDeletionDate
        };
    }

    public async Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Get or create PropertyMastOld for this property
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        int propertyMastOldId;

        // Step 2: Check if PropertyMastOld exists or create it
        if (property.PropertyMastOldId.HasValue)
        {
            propertyMastOldId = property.PropertyMastOldId.Value;
        }
        else
        {
            // Auto-create PropertyMastOld record (consistent with UpdateOldDetailsAsync behavior)
            var newPropertyMastOld = new PropertyMastOldEntity
            {
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            await _context.PropertyMastOld.AddAsync(newPropertyMastOld, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            propertyMastOldId = newPropertyMastOld.Id;

            // Update PropertyMast with the new PropertyMastOldId
            var propertyEntity = await _context.PropertyMast.FindAsync(new object[] { propertyId }, cancellationToken);
            if (propertyEntity != null)
            {
                propertyEntity.PropertyMastOldId = propertyMastOldId;
                propertyEntity.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Step 3: Validate foreign keys
        if (dto.OldFloorId.HasValue)
        {
            var floorExists = await _context.FloorEntity
                .AnyAsync(f => f.Id == dto.OldFloorId.Value && f.IsActive, cancellationToken);
            if (!floorExists)
            {
                throw new InvalidOperationException($"Invalid or inactive Floor ID: {dto.OldFloorId.Value}");
            }
        }

        if (dto.OldSubFloorId.HasValue)
        {
            var subFloorExists = await _context.SubFloorEntity
                .AnyAsync(sf => sf.Id == dto.OldSubFloorId.Value && sf.IsActive, cancellationToken);
            if (!subFloorExists)
            {
                throw new InvalidOperationException($"Invalid or inactive SubFloor ID: {dto.OldSubFloorId.Value}");
            }
        }

        if (dto.OldConstructionTypeId.HasValue)
        {
            var constructionTypeExists = await _context.ConstructionTypeEntity
                .AnyAsync(c => c.Id == dto.OldConstructionTypeId.Value && c.IsActive, cancellationToken);
            if (!constructionTypeExists)
            {
                throw new InvalidOperationException($"Invalid or inactive ConstructionType ID: {dto.OldConstructionTypeId.Value}");
            }
        }

        if (dto.OldTypeOfUseId.HasValue)
        {
            var typeOfUseExists = await _context.TypeOfUse
                .AnyAsync(t => t.Id == dto.OldTypeOfUseId.Value && t.IsActive, cancellationToken);
            if (!typeOfUseExists)
            {
                throw new InvalidOperationException($"Invalid or inactive TypeOfUse ID: {dto.OldTypeOfUseId.Value}");
            }
        }

        if (dto.OldSubTypeOfUseId.HasValue)
        {
            var subTypeOfUseExists = await _context.SubTypeOfUse
                .AnyAsync(stu => stu.Id == dto.OldSubTypeOfUseId.Value && stu.IsActive, cancellationToken);
            if (!subTypeOfUseExists)
            {
                throw new InvalidOperationException($"Invalid or inactive SubTypeOfUse ID: {dto.OldSubTypeOfUseId.Value}");
            }
        }

        // Step 4: Create new entity
        var newEntity = new PropertyDetailsOldEntity
        {
            PropertyMastOldId = propertyMastOldId,
            OldFloorId = dto.OldFloorId,
            OldSubFloorId = dto.OldSubFloorId,
            OldConstructionYear = dto.OldConstructionYear,
            OldAssessmentYear = dto.OldAssessmentYear,
            OldConstructionTypeId = dto.OldConstructionTypeId,
            OldTypeOfUseId = dto.OldTypeOfUseId,
            OldSubTypeOfUseId = dto.OldSubTypeOfUseId,
            OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter,
            OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet,
            OldBuiltupAreaSqMeter = dto.OldBuiltupAreaSqMeter,
            OldBuiltupAreaSqFeet = dto.OldBuiltupAreaSqFeet,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };

        await _context.PropertyDetailsOld.AddAsync(newEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Step 5: Return the newly created record with joined data
        var query = from pd in _context.PropertyDetailsOld
                    where pd.Id == newEntity.Id

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

                    select new
                    {
                        Id = pd.Id,
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

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new PropertyDetailsOldDto
        {
            Id = result.Id,
            PropertyId = propertyId,
            OldFloorId = result.OldFloorId,
            FloorDescription = result.FloorDescription,
            OldSubFloorId = result.OldSubFloorId,
            SubFloorDescription = result.SubFloorDescription,
            OldConstructionYear = result.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(result.OldConstructionYear) && int.TryParse(result.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = result.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(result.OldAssessmentYear) && int.TryParse(result.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = result.OldConstructionTypeId,
            ConstructionTypeDescription = result.ConstructionTypeDescription,
            OldTypeOfUseId = result.OldTypeOfUseId,
            TypeOfUseDescription = result.TypeOfUseDescription,
            OldSubTypeOfUseId = result.OldSubTypeOfUseId,
            SubTypeOfUseDescription = result.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = result.OldCarpetAreaSqMeter.HasValue ? Math.Round(result.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = result.OldCarpetAreaSqFeet.HasValue ? Math.Round(result.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = result.OldBuiltupAreaSqMeter.HasValue ? Math.Round(result.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = result.OldBuiltupAreaSqFeet.HasValue ? Math.Round(result.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = result.MarkedForDeletion,
            MarkedForDeletionDate = result.MarkedForDeletionDate
        };
    }

    public async Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
            throw new InvalidOperationException($"Property {propertyId} does not have an associated PropertyMastOld record");

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Get the existing floor record
        var existingRecord = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.Id == floorId && pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion, cancellationToken);

        if (existingRecord == null)
            return null;

        // Step 3: Validate foreign keys
        if (dto.OldFloorId.HasValue)
        {
            var floorExists = await _context.FloorEntity
                .AnyAsync(f => f.Id == dto.OldFloorId.Value && f.IsActive, cancellationToken);
            if (!floorExists)
            {
                throw new InvalidOperationException($"Invalid or inactive Floor ID: {dto.OldFloorId.Value}");
            }
        }

        if (dto.OldSubFloorId.HasValue)
        {
            var subFloorExists = await _context.SubFloorEntity
                .AnyAsync(sf => sf.Id == dto.OldSubFloorId.Value && sf.IsActive, cancellationToken);
            if (!subFloorExists)
            {
                throw new InvalidOperationException($"Invalid or inactive SubFloor ID: {dto.OldSubFloorId.Value}");
            }
        }

        if (dto.OldConstructionTypeId.HasValue)
        {
            var constructionTypeExists = await _context.ConstructionTypeEntity
                .AnyAsync(c => c.Id == dto.OldConstructionTypeId.Value && c.IsActive, cancellationToken);
            if (!constructionTypeExists)
            {
                throw new InvalidOperationException($"Invalid or inactive ConstructionType ID: {dto.OldConstructionTypeId.Value}");
            }
        }

        if (dto.OldTypeOfUseId.HasValue)
        {
            var typeOfUseExists = await _context.TypeOfUse
                .AnyAsync(t => t.Id == dto.OldTypeOfUseId.Value && t.IsActive, cancellationToken);
            if (!typeOfUseExists)
            {
                throw new InvalidOperationException($"Invalid or inactive TypeOfUse ID: {dto.OldTypeOfUseId.Value}");
            }
        }

        if (dto.OldSubTypeOfUseId.HasValue)
        {
            var subTypeOfUseExists = await _context.SubTypeOfUse
                .AnyAsync(stu => stu.Id == dto.OldSubTypeOfUseId.Value && stu.IsActive, cancellationToken);
            if (!subTypeOfUseExists)
            {
                throw new InvalidOperationException($"Invalid or inactive SubTypeOfUse ID: {dto.OldSubTypeOfUseId.Value}");
            }
        }

        // Step 4: Update the entity
        existingRecord.OldFloorId = dto.OldFloorId;
        existingRecord.OldSubFloorId = dto.OldSubFloorId;
        existingRecord.OldConstructionYear = dto.OldConstructionYear;
        existingRecord.OldAssessmentYear = dto.OldAssessmentYear;
        existingRecord.OldConstructionTypeId = dto.OldConstructionTypeId;
        existingRecord.OldTypeOfUseId = dto.OldTypeOfUseId;
        existingRecord.OldSubTypeOfUseId = dto.OldSubTypeOfUseId;
        existingRecord.OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter;
        existingRecord.OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet;
        existingRecord.OldBuiltupAreaSqMeter = dto.OldBuiltupAreaSqMeter;
        existingRecord.OldBuiltupAreaSqFeet = dto.OldBuiltupAreaSqFeet;
        existingRecord.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        // Step 5: Return the updated record with joined data
        var query = from pd in _context.PropertyDetailsOld
                    where pd.Id == floorId

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

                    select new
                    {
                        Id = pd.Id,
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

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new PropertyDetailsOldDto
        {
            Id = result.Id,
            PropertyId = propertyId,
            OldFloorId = result.OldFloorId,
            FloorDescription = result.FloorDescription,
            OldSubFloorId = result.OldSubFloorId,
            SubFloorDescription = result.SubFloorDescription,
            OldConstructionYear = result.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(result.OldConstructionYear) && int.TryParse(result.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = result.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(result.OldAssessmentYear) && int.TryParse(result.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = result.OldConstructionTypeId,
            ConstructionTypeDescription = result.ConstructionTypeDescription,
            OldTypeOfUseId = result.OldTypeOfUseId,
            TypeOfUseDescription = result.TypeOfUseDescription,
            OldSubTypeOfUseId = result.OldSubTypeOfUseId,
            SubTypeOfUseDescription = result.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = result.OldCarpetAreaSqMeter.HasValue ? Math.Round(result.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = result.OldCarpetAreaSqFeet.HasValue ? Math.Round(result.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = result.OldBuiltupAreaSqMeter.HasValue ? Math.Round(result.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = result.OldBuiltupAreaSqFeet.HasValue ? Math.Round(result.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = result.MarkedForDeletion,
            MarkedForDeletionDate = result.MarkedForDeletionDate
        };
    }

    public async Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return false;

        if (!property.PropertyMastOldId.HasValue)
            return false;

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Get the existing floor record
        var existingRecord = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.Id == floorId && pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion, cancellationToken);

        if (existingRecord == null)
            return false;

        // Step 3: Soft delete the record
        existingRecord.MarkedForDeletion = true;
        existingRecord.IsActive = false;
        existingRecord.MarkedForDeletionDate = DateTime.Now;
        existingRecord.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
    public async Task<PropertyTaxApartmentDetailsDto?> GetAggregatedPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPropertyNo = string.IsNullOrWhiteSpace(dto.PropertyNo) ? null : dto.PropertyNo.ToLower();
        var normalizedPartType = string.IsNullOrWhiteSpace(dto.PartType) ? null : dto.PartType.ToLower();
        var propertyIds = await (from pm in _context.PropertyMast.AsNoTracking()
                                 join pt in _context.PropertyTypeMasters on pm.PropertyTypeId equals pt.Id
                                 where (dto.WardId == null || pm.WardId == dto.WardId) &&
                                       (normalizedPropertyNo == null || (pm.PropertyNo != null && pm.PropertyNo.ToLower().Contains(normalizedPropertyNo))) &&
                                       (normalizedPartType == null || (pt.PartType != null && pt.PartType.ToLower().Contains(normalizedPartType))) &&
                                       (dto.PropertyId == null || pm.Id == dto.PropertyId) &&
                                       pm.IsActive && !pm.MarkedForDeletion &&
                                       pt.IsActive
                                 select pm.Id)
                                .ToListAsync(cancellationToken);

        if (propertyIds == null || !propertyIds.Any())
            return null;

        var taxData = await (from tmrv in _context.TransMastRV
                             join tm in _context.TaxMaster on tmrv.TaxId equals tm.Id
                             join ym in _context.YearMaster on tmrv.FinanceYearId equals ym.Id
                             where propertyIds.Contains(tmrv.PropertyId)
                                && tmrv.IsActive && !tmrv.MarkedForDeletion
                                && tm.IsActive
                                && ym.IsActive
                             orderby tm.DisplayOrder
                             select new
                             {
                                 TaxName = tm.TaxName,
                                 TaxAmount = tmrv.TaxAmount,
                                 DisplayOrder = tm.DisplayOrder
                             })
                            .ToListAsync(cancellationToken);

        if (!taxData.Any())
            return null;

        var taxAmountList = taxData
            .GroupBy(x => new { x.TaxName, x.DisplayOrder })
            .Select(g => new TaxAmountDto
            {
                TaxName = g.Key.TaxName,
                TaxAmount = g.Sum(x => x.TaxAmount),
                DisplayOrder = g.Key.DisplayOrder
            })
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        return new PropertyTaxApartmentDetailsDto
        {
            PropertyId = propertyIds.Count == 1 ? propertyIds[0] : 0,
            PropertyCount = propertyIds.Count,
            TaxAmounts = taxAmountList
        };
    }

    public async Task<PropertyTaxApartmentDetailsCVDto?> GetAggregatedPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPropertyNo = string.IsNullOrWhiteSpace(dto.PropertyNo) ? null : dto.PropertyNo.ToLower();
        var normalizedPartType = string.IsNullOrWhiteSpace(dto.PartType) ? null : dto.PartType.ToLower();
        var propertyIds = await (from pm in _context.PropertyMast.AsNoTracking()
                                 join pt in _context.PropertyTypeMasters on pm.PropertyTypeId equals pt.Id
                                 where (dto.WardId == null || pm.WardId == dto.WardId) &&
                                       (normalizedPropertyNo == null || (pm.PropertyNo != null && pm.PropertyNo.ToLower().Contains(normalizedPropertyNo))) &&
                                       (normalizedPartType == null || (pt.PartType != null && pt.PartType.ToLower().Contains(normalizedPartType))) &&
                                       (dto.PropertyId == null || pm.Id == dto.PropertyId) &&
                                       pm.IsActive && !pm.MarkedForDeletion &&
                                       pt.IsActive
                                 select pm.Id)
                                .ToListAsync(cancellationToken);

        if (propertyIds == null || !propertyIds.Any())
            return null;

        var taxData = await (from tmcv in _context.TransMastCV
                             join tm in _context.TaxMaster on tmcv.TaxId equals tm.Id
                             join ym in _context.YearMaster on tmcv.FinanceYearId equals ym.Id
                             where propertyIds.Contains(tmcv.PropertyId)
                                && tmcv.IsActive && !tmcv.MarkedForDeletion
                                && tm.IsActive
                                && ym.IsActive
                             orderby tm.DisplayOrder
                             select new
                             {
                                 TaxName = tm.TaxName,
                                 TaxAmount = tmcv.TaxAmount,
                                 DisplayOrder = tm.DisplayOrder
                             })
                            .ToListAsync(cancellationToken);

        if (!taxData.Any())
            return null;

        var taxAmountList = taxData
            .GroupBy(x => new { x.TaxName, x.DisplayOrder })
            .Select(g => new TaxAmountDto
            {
                TaxName = g.Key.TaxName,
                TaxAmount = g.Sum(x => x.TaxAmount),
                DisplayOrder = g.Key.DisplayOrder
            })
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        return new PropertyTaxApartmentDetailsCVDto
        {
            PropertyId = propertyIds.Count == 1 ? propertyIds[0] : 0,
            PropertyCount = propertyIds.Count,
            TaxAmounts = taxAmountList
        };
    }


    public async Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default)
    {
        int iFromFloor = 1;
        int iToFloor = 1;
        int number;


        if (dto.GenerationType.ToLower() == "HC".ToLower() & dto.FromFloor != dto.ToFloor)
        {
            throw new InvalidOperationException("From floor and to floor must be same");
        }
        else if (dto.GenerationType.ToLower() == "VC".ToLower() & dto.NoOfFlatOnOneFloor > 1)
        {
            throw new InvalidOperationException("Vertical Custom Generation no of flat in one floor must be 1");
        }


        if (int.TryParse(dto.FromFloor, out number) && number >= 1 && number <= 1000)
        {
            iFromFloor = Convert.ToInt32(dto.FromFloor);
            iToFloor = Convert.ToInt32(dto.ToFloor);

            if (iFromFloor > iToFloor)
            {
                throw new InvalidOperationException("From Floor cannot be greater than To Floor");
            }
        }

        else
        {

            if (dto.GenerationType.ToLower() != "hc" && dto.GenerationType.ToLower() != "vc")
            {
                throw new InvalidOperationException("Select horizontal custom or vertical custom for generation");
            }


        }


        // Step 1: Validate input parameters


        if (dto.NoOfFlatOnOneFloor <= 0)
        {
            throw new InvalidOperationException("No Of Flat On One Floor must be greater than zero");
        }

        if (dto.Prifix != "" && dto.Prifix != null)
        {
            dto.Prifix = dto.Prifix + "-";
        }

        // Step 2: Validate WingId exists and get WingNo
        var wingNo = await _context.Set<WingEntity>()
            .Where(w => w.Id == dto.WingId && w.IsActive)
            .Select(w => w.WingNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (wingNo == null)
        {
            throw new InvalidOperationException("Wing Not Found");
        }

        // Step 3: Get existing property count for partition number calculation
        // This is equivalent to @LastPropertyNo in the SQL query
        var lastPropertyNo = await (from p in _context.PropertyMast
                                    join s in _context.SocietyDetailsMast on p.SocietyDetailId equals s.Id
                                    where p.WardId == dto.WardId
                                          && p.PropertyNo == dto.PropertyNo
                                          && s.WingId == dto.WingId
                                          && p.IsActive
                                          && !p.MarkedForDeletion
                                          && s.IsActive
                                          && !s.MarkedForDeletion
                                    select p).CountAsync(cancellationToken);

        // Step 4: Generate floor and unit sequences (equivalent to CTEs in SQL)
        // Floors CTE: SELECT @FromFloor AS FloorNo UNION ALL SELECT FloorNo + 1 FROM Floors WHERE FloorNo < @ToFloor
        var floors = Enumerable.Range(iFromFloor, iToFloor - iFromFloor + 1).ToList();

        // Units CTE: SELECT 1 AS UnitNo UNION ALL SELECT UnitNo + 1 FROM Units WHERE UnitNo < @NoOfFlatOnOneFloor
        var units = Enumerable.Range(1, dto.NoOfFlatOnOneFloor).ToList();

        // Step 5: Vertical Generation - Cross join ordered by UnitNo, then FloorNo

        // Determine generation type flags
        var isVertical = dto.GenerationType.Equals("V", StringComparison.OrdinalIgnoreCase) ||
                         dto.GenerationType.Equals("VC", StringComparison.OrdinalIgnoreCase);
        var isHorizontal = dto.GenerationType.Equals("H", StringComparison.OrdinalIgnoreCase) ||
                           dto.GenerationType.Equals("HC", StringComparison.OrdinalIgnoreCase);
        var isHC = dto.GenerationType.Equals("HC", StringComparison.OrdinalIgnoreCase);

        if (!isVertical && !isHorizontal)
        {
            throw new InvalidOperationException("Invalid Generation Type");
        }

        // Normalize prefix
        var prefix = !string.IsNullOrEmpty(dto.Prifix) ? $"{dto.Prifix}-" : string.Empty;
        var normalizedType = dto.GenerationType.ToUpperInvariant();

        // Create cross join of units and floors
        var crossJoin = from u in units
                        from f in floors
                        select (FloorNo: f, UnitNo: u);

        // Apply ordering based on generation type
        // Vertical (V, VC): order by UnitNo then FloorNo
        // Horizontal (H, HC): order by FloorNo then UnitNo
        var orderedItems = isVertical
            ? crossJoin.OrderBy(x => x.UnitNo).ThenBy(x => x.FloorNo)
            : crossJoin.OrderBy(x => x.FloorNo).ThenBy(x => x.UnitNo);

        // Generate result with floor multiplier (HC uses 0, others use FloorNo - 1)
        return orderedItems
            .Select((item, index) => new BuildingGenerateStructureDto
            {
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo,
                WingId = dto.WingId,
                RowNo = index + 1,
                FloorNo = item.FloorNo,
                UnitNo = item.UnitNo,
                FlatNo = $"{prefix}{dto.FlatStart + (isHC ? 0 : (item.FloorNo - 1) * dto.IncrementedBy) + (item.UnitNo - 1)}",
                PartitionNo = $"{wingNo}{index + 1 + lastPropertyNo}",
                GenerationType = normalizedType
            })
            .ToList();

    }


    public async Task<List<BuildingListDto>?> GetBuildingListAsync(int wardId, CancellationToken cancellationToken = default)
    {
        var WardDetails = await _context.WardMaster
     .Where(p => p.Id == wardId && p.IsActive)
     .Select(p => new { p.Id })
     .FirstOrDefaultAsync(cancellationToken);

        if (WardDetails == null)
            return null;

        // Step 1: Query builing list properties as per ward
        var buildingProperties = await (from pm in _context.PropertyMast
                                        join pcm in _context.PropertyCategoryMaster on pm.CategoryId equals pcm.Id
                                        join wm in _context.WardMaster on pm.WardId equals wm.Id
                                        where pm.WardId == wardId
                                        && string.IsNullOrEmpty(pm.PartitionNo)
                                         && pm.IsActive
                                         && !pm.MarkedForDeletion
                                         && wm.IsActive
                                         && pcm.IsActive

                                        select new BuildingListDto
                                        {
                                            PropertyId = pm.Id,
                                            WardNo = wm.WardNo,
                                            CatPropertyCategoryName = pcm.PropertyCategoryName,
                                            PropertyNo = pm.PropertyNo,
                                            PartitionNo = pm.PartitionNo
                                        })
                                      .ToListAsync(cancellationToken);


        return buildingProperties;
    }


    public async Task<CreateNewPropertyResponseDto?> CreateNewPropertyAsync(CreateNewPropertyDto dto, CancellationToken cancellationToken = default)
    {
        // Null check for request body
        ArgumentNullException.ThrowIfNull(dto);

        var propertyExists = await _context.PropertyMast.AnyAsync(x => x.PropertyNo == dto.PropertyNo && x.WardId == dto.WardId && x.PartitionNo == "", cancellationToken);

        if (propertyExists)
            return new CreateNewPropertyResponseDto
            {
                Success = false,
                Message = string.Join(" ", "PropertyNo already exists in our records.")
            };

        SocietyDetailsEntity? society = null;
        PropertyEntity? property = null;
        PropertyAssessmentEntity? propertyMastDetails = null;
        PropertyDetailsEntity? propertyDetails = null;
        RoomWiseSubmissionDetailsEntity? RoomWiseSubmissionDetails = null;

        try
        {
            var ward = await _context.WardMaster.FirstOrDefaultAsync(x => x.Id == dto.WardId, cancellationToken);

            // ============ STEP 2: Property insert ============
            property = new PropertyEntity
            {
                TaxZoneId = dto.TaxZoneId,
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo?.Trim(),
                PartitionNo = dto.PartitionNo,
                PropertySeqNo = dto.PropertySeqNo,
                PropertyTypeId = dto.PropertyTypeId,
                OpenPlot = dto.OpenPlot,
                CSN = dto.CSN,
                PlotNo = dto.PlotNo,
                CategoryId = dto.CategoryId,
                Type = dto.Type,
                OwnerTitle = dto.OwnerTitle,
                OwnerTitleEnglish = dto.OwnerTitleEnglish,
                OwnerName = dto.OwnerName,
                OwnerNameEnglish = dto.OwnerNameEnglish,
                MobileNo = dto.MobileNo,
                UPICId = ward != null ? $"{dto.PropertyNo}{dto.WardId}{dto.PartitionNo}{ward.WardNo}" : $"{dto.PropertyNo}{dto.WardId}{dto.PartitionNo}",
                EmailId = dto.EmailId,
                OccupierTitle = dto.OccupierTitle,
                OccupierTitleEnglish = dto.OccupierTitleEnglish,
                OccupierName = dto.OccupierName,
                OccupierNameEnglish = dto.OccupierNameEnglish,
                FlatOrShopNo = dto.FlatOrShopNo,
                FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish,
                FlatOrShopNameEnglish = dto.FlatOrShopNameEnglish,
                FlatOrShopName = dto.FlatOrShopName,
                Address = dto.Address,
                AddressEnglish = dto.AddressEnglish,
                AlternateMobileNo = dto.AlternateMobileNo,
                MobileNoRemarkId = dto.MobileNoRemarkId,
                OccupierMobileNo = dto.OccupierMobileNo,
                OccupierMobileNoRemarkId = dto.OccupierMobileNoRemarkId,
                PropertyMastOldId = dto.PropertyMastOldId,
                PinCode = dto.PinCode,
                Location = dto.Location,
                LocationEnglish = dto.LocationEnglish,
                PropertyAssessmentStatusId = dto.PropertyAssessmentStatusId,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto.CreatedBy
            };

            _context.PropertyMast.Add(property);
            await _context.SaveChangesAsync(cancellationToken);

            // ============ STEP 1: Society (Apartment only) ============

            var category = await _context.PropertyCategoryMaster.FirstOrDefaultAsync(x => x.Id == dto.CategoryId, cancellationToken);

            if (category != null && category.PropertyCategoryName.Contains("apartment"))
            {
                society = new SocietyDetailsEntity
                {
                    PropertyId = property.Id,
                    SocietyName = dto.SocietyName,
                    SocietyAddress = dto.SocietyAddress,
                    SecretaryName = dto.SecretaryName,
                    ManagerName = dto.ManagerName,
                    LandOwnerName = dto.LandOwnerName,
                    BuilderName = dto.BuilderName,
                    BuilderMobileNo = dto.BuilderMobileNo,
                    BuilderMobileNoRemarkId = dto.BuilderMobileRemarkId,
                    SocietyNameEnglish = dto.SocietyNameEnglish,
                    SocietyAddressEnglish = dto.SocietyAddressEnglish,
                    SecretaryNameEnglish = dto.SecretaryNameEnglish,
                    LandOwnerNameEnglish = dto.LandOwnerNameEnglish,
                    ManagerNameEnglish = dto.ManagerNameEnglish,
                    BuilderNameEnglish = dto.BuilderNameEnglish,
                    ManagerMobileNo = dto.ManagerMobileNo,
                    SecretaryMobileNo = dto.SecretaryMobileNo,
                    SocietyEmailId = dto.SocietyEmailId,
                    SecretaryEmailId = dto.SecretaryEmailId,
                    ManagerEmailId = dto.ManagerEmailId,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = dto.CreatedBy
                };

                _context.SocietyDetailsMast.Add(society);
                await _context.SaveChangesAsync(cancellationToken);


                // ============ STEP 3: Link society to property ============
                property.SocietyDetailId = society.Id;
                _context.PropertyMast.Update(property);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // ============ STEP 4: Assessment insert ============
            propertyMastDetails = new PropertyAssessmentEntity
            {
                PropertyId = property.Id,
                SurveyRemark = dto.SurveyRemark,
                BlockNo = dto.BlockNo,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto.CreatedBy
            };

            _context.PropertyMastDetails.Add(propertyMastDetails);
            await _context.SaveChangesAsync(cancellationToken);

            // ============ STEP 5: PropertyDetails insert (PLOT category only) ============
            if (category != null && category.PropertyCategoryName.ToLower().Trim() == "plot")
            {
                // Check property details already present in our records or not 
                propertyDetails = await _context.PropertyDetails.FirstOrDefaultAsync(x => x.PropertyId == property.Id, cancellationToken);
                if (propertyDetails == null)
                {
                    propertyDetails = new PropertyDetailsEntity
                    {
                        PropertyId = property.Id,
                        FloorId = 1,
                        ConstructionTypeId = 1,
                        TypeOfUseId = 1,
                        IsActive = true,
                        MarkedForDeletion = false,
                    };

                    _context.PropertyDetails.Add(propertyDetails);
                    var propertyDetailsSaveResult = await _context.SaveChangesAsync(cancellationToken);
                    if (propertyDetailsSaveResult > 0)
                    {
                        RoomWiseSubmissionDetails = new RoomWiseSubmissionDetailsEntity
                        {
                            PropertyId = property.Id,
                            PropertyDetailsId = propertyDetails.Id,
                            LengthMtr = dto.LengthMtr,
                            WidthMtr = dto.WidthMtr,
                            TotalAreaSqMtr = dto.TotalAreaSqMtr,
                            CreatedBy = dto.CreatedBy,
                            CreatedDate = dto.CreatedDate,
                            OuterYesNo = false,
                            MinusYesNo = false,
                            IsActive = true,
                            MarkedForDeletion = false
                        };

                        _context.RoomWiseSubmissionDetails.Add(RoomWiseSubmissionDetails);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            return new CreateNewPropertyResponseDto
            {
                PropertyId = property.Id,
                UPICID = property.UPICId,
                WardID = property.WardId,
                Success = true,
                Message = "Property generated successfully."
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Failure($"A concurrency conflict occurred. Please retry. Detail: {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            return message switch
            {
                var m when ContainsAny(m, "duplicate", "unique")
                    => Failure("PropertyNo already exists. A duplicate was detected at the Records level."),

                var m when m.Contains("FK_RoomWiseSubmissionDetails_PropertyDetails", StringComparison.OrdinalIgnoreCase)
                    => Failure("PropertyDetailsId is invalid. PropertyDetails record does not exist for this Property."),

                var m when m.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
                    => Failure($"A referenced record does not exist. Please verify all IDs are valid. Detail: {message}"),

                var m when ContainsAny(m, "NULL", "not-null", "not allow nulls")
                    => Failure($"A required field is missing at the database level. Please check all mandatory fields. Detail: {message}"),

                _ => Failure($"A database error occurred while saving. Detail: {message}")
            };
        }
        catch (OperationCanceledException)
        {
            return Failure("The operation was cancelled before it could complete.");
        }
        catch (Exception ex)
        {
            return Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(
    int SocietyDetailId,
    bool isAmenity,
    CancellationToken cancellationToken = default)
    {
        var amenityProperties = await (
            from pm in _context.PropertyMast
            join ptm in _context.PropertyTypeMasters on pm.PropertyTypeId equals ptm.Id
            join wm in _context.WardMaster on pm.WardId equals wm.Id
            join sdm in _context.SocietyDetailsMast on pm.SocietyDetailId equals sdm.Id
            join we in _context.WingEntity on sdm.WingId equals we.Id
            where pm.SocietyDetailId == SocietyDetailId
    && !string.IsNullOrEmpty(pm.PartitionNo)
    && pm.PartitionNo != we.WingNo
    && pm.MarkedForDeletion != true
    && pm.IsActive == true
    && (
                        isAmenity
                            ? ptm.PartType == PartTypeConstants.Amenity
                            : ptm.PartType != PartTypeConstants.Amenity
                     )
            orderby pm.Id descending

            select new SocietyAminityDetailsDto
            {
                PropertyId = pm.Id,
                SocietyDetailId = pm.SocietyDetailId ?? 0,
                WardId = pm.WardId,
                WardNo = wm.WardNo,
                wingId = we.Id,
                WingNo = we.WingNo,
                WingName = sdm.WingName,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                PartType = ptm.PartType
            })
            .ToListAsync(cancellationToken);

        return amenityProperties;
    }




    public async Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var property = await (
        from p in _context.PropertyMast
        join w in _context.WardMaster
            on p.WardId equals w.Id
        where p.Id == propertyId
              && p.IsActive
              && !p.MarkedForDeletion
        select new
        {
            p.Id,
            WardId = w.Id,
            WardNo = w.WardNo,
            PropertyNo = p.PropertyNo
        }
    ).FirstOrDefaultAsync(cancellationToken);


        if (property == null)
            return null;

        var amenityProperties = await (
      from sdm in _context.SocietyDetailsMast
      join we in _context.WingEntity on sdm.WingId equals we.Id into wingJoin
      from we in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
      where sdm.PropertyId == property.Id
            && sdm.IsActive
            && !sdm.MarkedForDeletion
      select new PropertySocietyDetailsDto
      {
          PropertyId = sdm.PropertyId,
          SocietyDetailId = sdm.Id,
          WingId = sdm.WingId,
          WingNo = we != null ? we.WingNo : null,
          WardNo = property.WardNo,
          PropertyNo = property.PropertyNo,
          WingName = sdm.WingName,
          SocietyName = sdm.SocietyName,
          SocietyAddress = sdm.SocietyAddress,
          SecretaryName = sdm.SecretaryName,
          ManagerName = sdm.ManagerName,
          LandOwnerName = sdm.LandOwnerName,
          BuilderName = sdm.BuilderName,
          SocietyNameEnglish = sdm.SocietyNameEnglish,
          SocietyAddressEnglish = sdm.SocietyAddressEnglish,
          SecretaryNameEnglish = sdm.SecretaryNameEnglish,
          ManagerNameEnglish = sdm.ManagerNameEnglish,
          LandOwnerNameEnglish = sdm.LandOwnerNameEnglish,
          BuilderNameEnglish = sdm.BuilderNameEnglish,
          ManagerMobileNo = sdm.ManagerMobileNo,
          SecretaryMobileNo = sdm.SecretaryMobileNo,
          SocietyEmailId = sdm.SocietyEmailId,
          SecretaryEmailId = sdm.SecretaryEmailId,
          ManagerEmailId = sdm.ManagerEmailId,
          PropertyCount = _context.PropertyMast
              .Where(pm => pm.SocietyDetailId == sdm.Id
                  && !string.IsNullOrEmpty(pm.PartitionNo)
                  && pm.IsActive
                  && !pm.MarkedForDeletion)
              .Join(_context.PropertyTypeMasters,
                  pm => pm.PropertyTypeId,
                  ptm => ptm.Id,
                  (pm, ptm) => ptm)
              .Count(ptm => ptm.PartType != PartTypeConstants.Amenity && ptm.IsActive),
          AminityCount = _context.PropertyMast
              .Where(pm => pm.SocietyDetailId == sdm.Id
                  && !string.IsNullOrEmpty(pm.PartitionNo)
                  && pm.IsActive
                  && !pm.MarkedForDeletion)
              .Join(_context.PropertyTypeMasters,
                  pm => pm.PropertyTypeId,
                  ptm => ptm.Id,
                  (pm, ptm) => ptm)
              .Count(ptm => ptm.PartType == PartTypeConstants.Amenity && ptm.IsActive),
      })
      .AsNoTracking()
      .ToListAsync(cancellationToken);
        return amenityProperties;
    }

    public async Task<bool> IsPropertyExists(int wardId, string propertyNo, int? propertyId)
    {
        return await _context.PropertyMast.AnyAsync(x => x.WardId == wardId && x.PropertyNo == propertyNo && (!propertyId.HasValue || x.Id != propertyId.Value));
    }

    public async Task<(int TotalCount, List<PropertySearchResponseDto> Items)> SearchPropertiesAsync(PropertySearchRequestDto searchRequest, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // Handle dashboard card filters
        if (searchRequest.DashboardFilter.HasValue)
        {
            switch (searchRequest.DashboardFilter.Value)
            {
                case DashboardFilterType.RegisteredProperty:
                    // Show all registered properties - no additional filter needed
                    break;

                case DashboardFilterType.GeoSequencing:
                    // Show properties where PropertyNo is present - will be applied below
                    break;

                case DashboardFilterType.Survey:
                case DashboardFilterType.DataProcessing:
                case DashboardFilterType.QualityAnalysis:
                case DashboardFilterType.AssessmentCompleted:
                    // These are work in progress - return empty result
                    return (0, new List<PropertySearchResponseDto>());
            }
        }

        // Handle property process filter (Type dropdown)
        if (searchRequest.PropertyProcessFilter.HasValue)
        {
            switch (searchRequest.PropertyProcessFilter.Value)
            {
                case PropertyProcessFilterType.SurveyCompleted:
                case PropertyProcessFilterType.DataEntryCompleted:
                case PropertyProcessFilterType.QCCompleted:
                case PropertyProcessFilterType.NoticeDistributed:
                    // All these are work in progress - return empty result
                    return (0, new List<PropertySearchResponseDto>());
            }
        }

        // Build the base query with all joins
        var query = from p in _context.PropertyMast.AsNoTracking()
                    where p.IsActive && !p.MarkedForDeletion

                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join z in _context.ZoneMaster.AsNoTracking() on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
                    from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join pmo in _context.PropertyMastOld.AsNoTracking() on p.PropertyMastOldId equals pmo.Id into oldJoin
                    from pmo in oldJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sd in _context.SocietyDetailsMast.AsNoTracking() on p.SocietyDetailId equals sd.Id into societyJoin
                    from sd in societyJoin.Where(x => x.IsActive && !x.MarkedForDeletion).DefaultIfEmpty()

                    select new
                    {
                        Property = p,
                        Ward = w,
                        Zone = z,
                        Category = pc,
                        OldProperty = pmo,
                        Society = sd
                    };

        // Apply dashboard filter for geo-sequencing
        if (searchRequest.DashboardFilter == DashboardFilterType.GeoSequencing)
        {
            query = query.Where(x => !string.IsNullOrEmpty(x.Property.PropertyNo));
        }

        // Apply Quick Search filters
        if (searchRequest.PropertyTypeId.HasValue)
        {
            query = query.Where(x => x.Property.PropertyTypeId == searchRequest.PropertyTypeId.Value);
        }

        if (searchRequest.CategoryId.HasValue)
        {
            query = query.Where(x => x.Property.CategoryId == searchRequest.CategoryId.Value);
        }

        if (searchRequest.TypeOfUseId.HasValue)
        {
            // TypeOfUse is in PropertyDetails, need to check if any PropertyDetails has this TypeOfUseId
            var propertyIdsWithTypeOfUse = _context.PropertyDetails
                .Where(pd => pd.IsActive && pd.TypeOfUseId == searchRequest.TypeOfUseId.Value)
                .Select(pd => pd.PropertyId)
                .Distinct();

            query = query.Where(x => propertyIdsWithTypeOfUse.Contains(x.Property.Id));
        }

        if (searchRequest.ZoneId.HasValue)
        {
            query = query.Where(x => x.Zone != null && x.Zone.Id == searchRequest.ZoneId.Value);
        }

        if (searchRequest.WardId.HasValue)
        {
            query = query.Where(x => x.Property.WardId == searchRequest.WardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom) && !string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
        {
            query = query.Where(x => x.Property.PropertyNo != null &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoFrom) >= 0 &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoTo) <= 0);
        }
        else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoFrom))
        {
            query = query.Where(x => x.Property.PropertyNo != null &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoFrom) >= 0);
        }
        else if (!string.IsNullOrWhiteSpace(searchRequest.PropertyNoTo))
        {
            query = query.Where(x => x.Property.PropertyNo != null &&
                                   string.Compare(x.Property.PropertyNo, searchRequest.PropertyNoTo) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OldPropertyNo))
        {
            query = query.Where(x => x.OldProperty != null &&
                                   x.OldProperty.OldPropertyNo != null &&
                                   x.OldProperty.OldPropertyNo.Contains(searchRequest.OldPropertyNo));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.UPICId))
        {
            query = query.Where(x => x.Property.UPICId != null &&
                                   x.Property.UPICId.Contains(searchRequest.UPICId));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.CSN))
        {
            query = query.Where(x => x.Property.CSN != null &&
                                   x.Property.CSN.Contains(searchRequest.CSN));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.SubZoneNo))
        {
            query = query.Where(x => x.Property.SubZoneNo != null &&
                                   x.Property.SubZoneNo.Contains(searchRequest.SubZoneNo));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.PlotNo))
        {
            query = query.Where(x => x.Property.PlotNo != null &&
                                   x.Property.PlotNo.Contains(searchRequest.PlotNo));
        }

        if (searchRequest.PropertyAssessmentStatusId.HasValue)
        {
            query = query.Where(x => x.Property.PropertyAssessmentStatusId == searchRequest.PropertyAssessmentStatusId.Value);
        }

        // Apply KYC Search filters
        if (!string.IsNullOrWhiteSpace(searchRequest.MobileNo))
        {
            query = query.Where(x => (x.Property.MobileNo != null && x.Property.MobileNo.Contains(searchRequest.MobileNo)) ||
                                   (x.Property.AlternateMobileNo != null && x.Property.AlternateMobileNo.Contains(searchRequest.MobileNo)) ||
                                   (x.Property.OccupierMobileNo != null && x.Property.OccupierMobileNo.Contains(searchRequest.MobileNo)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OwnerName))
        {
            query = query.Where(x => (x.Property.OwnerName != null && x.Property.OwnerName.Contains(searchRequest.OwnerName)) ||
                                   (x.Property.OwnerNameEnglish != null && x.Property.OwnerNameEnglish.Contains(searchRequest.OwnerName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.OccupierName))
        {
            query = query.Where(x => (x.Property.OccupierName != null && x.Property.OccupierName.Contains(searchRequest.OccupierName)) ||
                                   (x.Property.OccupierNameEnglish != null && x.Property.OccupierNameEnglish.Contains(searchRequest.OccupierName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.FlatOrShopName))
        {
            query = query.Where(x => (x.Property.FlatOrShopName != null && x.Property.FlatOrShopName.Contains(searchRequest.FlatOrShopName)) ||
                                   (x.Property.FlatOrShopNo != null && x.Property.FlatOrShopNo.Contains(searchRequest.FlatOrShopName)) ||
                                   (x.Property.FlatOrShopNameEnglish != null && x.Property.FlatOrShopNameEnglish.Contains(searchRequest.FlatOrShopName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.SocietyName))
        {
            query = query.Where(x => (x.Society != null && x.Society.SocietyName != null && x.Society.SocietyName.Contains(searchRequest.SocietyName)) ||
                                   (x.Society != null && x.Society.SocietyNameEnglish != null && x.Society.SocietyNameEnglish.Contains(searchRequest.SocietyName)));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.Address))
        {
            query = query.Where(x => (x.Property.Address != null && x.Property.Address.Contains(searchRequest.Address)) ||
                                   (x.Property.AddressEnglish != null && x.Property.AddressEnglish.Contains(searchRequest.Address)));
        }

        // Apply Values & Dues Search Filters
        if (searchRequest.RVorCV != null && searchRequest.RVorCV.Trim().Length > 0)
        {
            var rvOrCv = searchRequest.RVorCV.Trim().ToUpper();

            query = query.Where(x => _context.TransMast.Any(t => t.PropertyId == x.Property.Id && t.IsActive && !t.MarkedForDeletion &&
                    t.RVorCV == rvOrCv));
        }

        if (!string.IsNullOrWhiteSpace(searchRequest.AmountFilterOperator) && searchRequest.AmountValue.HasValue)
        {
            if (!Enum.TryParse<FilterOperator>(searchRequest.AmountFilterOperator.Trim(),ignoreCase: true,out var op) ||
                !Enum.IsDefined(typeof(FilterOperator), op))
            {
                return (0, new List<PropertySearchResponseDto>());
            }

            var amount = searchRequest.AmountValue.Value;
            var applyAmountFilter = true;

            var taxQuery = _context.TransMast
                .Where(t => t.IsActive && !t.MarkedForDeletion)
                .GroupBy(t => t.PropertyId)
                .Select(g => new
                {
                    PropertyId = g.Key,
                    TotalTax = g.Sum(x => x.TaxAmount)
                });

            if (op == FilterOperator.Equals)
            {
                taxQuery = taxQuery.Where(t => t.TotalTax == amount);
            }
            else if (op == FilterOperator.GreaterThan)
            {
                taxQuery = taxQuery.Where(t => t.TotalTax > amount);
            }
            else if (op == FilterOperator.LessThan)
            {
                taxQuery = taxQuery.Where(t => t.TotalTax < amount);
            }
            else if (op == FilterOperator.Between && searchRequest.AmountTo.HasValue)
            {
                var toAmount = searchRequest.AmountTo.Value;

                taxQuery = taxQuery.Where(t =>
                    t.TotalTax >= amount &&
                    t.TotalTax <= toAmount);
            }
            else
            {
                applyAmountFilter = false;
            }

            if (applyAmountFilter)
            {
                query = query.Where(x =>
                    taxQuery.Any(t => t.PropertyId == x.Property.Id));
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply deterministic ordering before pagination to ensure stable paging
        var orderedQuery = query.OrderBy(x => x.Property.Id);

        // Apply pagination
        var isUnpaged = pageSize == -1;
        var skip = isUnpaged ? 0 : (pageNumber - 1) * pageSize;

        var pagedQuery = isUnpaged
            ? orderedQuery
            : orderedQuery.Skip(skip).Take(pageSize);

        var propertyResults = await pagedQuery.ToListAsync(cancellationToken);

        if (!propertyResults.Any())
        {
            return (totalCount, new List<PropertySearchResponseDto>());
        }

        var propertyIds = propertyResults.Select(x => x.Property.Id).ToList();

        // Get RV (Rateable Value) from TransMastRV table - get latest value per property
        var rvValues = await _context.TransMastRV
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                RateableValue = g.OrderByDescending(x => x.Id).Select(x => x.RateableValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Get CV (Capital Value) from TransMastCV table - get latest value per property
        var cvValues = await _context.TransMastCV
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                CapitalValue = g.OrderByDescending(x => x.Id).Select(x => x.CapitalValue).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Get Total Tax from TransMast table - sum all tax amounts per property
        var totalTaxAmounts = await _context.TransMast
            .Where(t => propertyIds.Contains(t.PropertyId) && t.IsActive && !t.MarkedForDeletion)
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                TotalTax = g.Sum(x => x.TaxAmount)
            })
            .ToListAsync(cancellationToken);

        // Convert to dictionaries for O(1) lookup performance
        var rvDictionary = rvValues.ToDictionary(x => x.PropertyId, x => x.RateableValue);
        var cvDictionary = cvValues.ToDictionary(x => x.PropertyId, x => x.CapitalValue);
        var totalTaxDictionary = totalTaxAmounts.ToDictionary(x => x.PropertyId, x => x.TotalTax);

        // Map to response DTOs
        var result = propertyResults.Select(pr =>
        {
            rvDictionary.TryGetValue(pr.Property.Id, out var rv);
            cvDictionary.TryGetValue(pr.Property.Id, out var cv);
            totalTaxDictionary.TryGetValue(pr.Property.Id, out var totalTax);

            return new PropertySearchResponseDto
            {
                PropertyId = pr.Property.Id,
                UPICId = pr.Property.UPICId,
                ZoneName = pr.Zone?.ZoneNo,
                WardName = pr.Ward?.WardNo,
                PropertyNo = pr.Property.PropertyNo,
                PartitionNo = pr.Property.PartitionNo,
                OldPropertyNo = pr.OldProperty?.OldPropertyNo,
                CitySurveyNo = pr.Property.CSN,
                PlotNo = pr.Property.PlotNo,
                WingFlatNo = pr.Property.FlatOrShopNo,
                CategoryName = pr.Category?.PropertyCategoryName,
                PropertyDescription = pr.Property.Type,
                Mobile = pr.Property.MobileNo,
                PropertyHolderName = pr.Property.OwnerName ?? pr.Property.OwnerNameEnglish,
                OccupierName = pr.Property.OccupierName ?? pr.Property.OccupierNameEnglish,
                ShopBuildingName = pr.Property.FlatOrShopName ?? pr.Property.FlatOrShopNameEnglish,
                SocietyName = pr.Society?.SocietyName ?? pr.Society?.SocietyNameEnglish,
                Address = pr.Property.Address ?? pr.Property.AddressEnglish,
                RV = rv,
                CV = cv,
                TotalTax = totalTax
            };
        }).ToList();

        return (totalCount, result);
    }

    public async Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        // Get all active properties
        var allProperties = _context.PropertyMast
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        // 1. Registered Property Count: All properties present in PropertyMast
        var registeredCount = await allProperties.CountAsync(cancellationToken);

        // 2. Geo Sequencing Property Count: Properties where PropertyNo is present
        var geoSequencingCount = await allProperties
            .Where(p => !string.IsNullOrEmpty(p.PropertyNo))
            .CountAsync(cancellationToken);

        // 3. Survey Property Count: Currently 0 (Work in Progress)
        var surveyCount = 0;

        // 4. Data Processing Property Count: Currently 0 (Work in Progress)
        var dataProcessingCount = 0;

        // 5. Quality Analysis Property Count: Currently 0 (Work in Progress)
        var qualityAnalysisCount = 0;

        // 6. Assessment Completed Property Count: Currently 0 (Work in Progress)
        var assessmentCompletedCount = 0;

        return new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = registeredCount,
            GeoSequencingPropertyCount = geoSequencingCount,
            SurveyPropertyCount = surveyCount,
            DataProcessingPropertyCount = dataProcessingCount,
            QualityAnalysisPropertyCount = qualityAnalysisCount,
            AssessmentCompletedPropertyCount = assessmentCompletedCount
        };
    }

    private static CreateNewPropertyResponseDto Failure(string message)
    {
        return new CreateNewPropertyResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        return values.Any(v =>
            source.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all RoomWiseMinusData entities by list of RoomWiseSubmissionId values.
    /// Used during property deletion to mark all minus data records for deletion.
    /// This entity only has RoomWiseSubmissionId column (no PropertyId), so we query by parent RoomWiseSubmissionDetails IDs.
    /// </summary>
    public async Task<List<RoomWiseMinusDataEntity>> GetRoomWiseMinusBySubmissionIdsAsync(List<int> roomWiseSubmissionIds, CancellationToken cancellationToken = default)
    {
        return await _context.RoomWiseMinusData
            .Where(x => roomWiseSubmissionIds.Contains(x.RoomWiseSubmissionId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets PropertyDetails entities for a property.
    /// Used as the first step in property deletion to identify related PropertyDetailsId values.
    /// </summary>
    public async Task<List<PropertyDetailsEntity>> GetPropertyDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDetails
            .Where(pd => pd.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #region PropertyTaxCalculationRVResults - Entity has BOTH PropertyId AND PropertyDetailsId

    /// <summary>
    /// Gets all PropertyTaxCalculationRVResults for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All RV results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    public async Task<List<PropertyTaxCalculationRVResultsEntity>> GetRvResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTaxCalculationRVResults
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region PropertyTaxCalculationSection129Results - Entity has BOTH PropertyId AND PropertyDetailsId

    /// <summary>
    /// Gets all PropertyTaxCalculationSection129Results for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All Section129 results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    public async Task<List<PropertyTaxCalculationSection129ResultsEntity>> GetSection129ResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTaxCalculationSection129Results
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entities with ONLY PropertyDetailsId (no PropertyId column)

    /// <summary>
    /// Gets PropertyOccupancyDetails by PropertyDetailsId list.
    /// This entity only has PropertyDetailId column (no PropertyId), so simple query is sufficient.
    /// </summary>
    public async Task<List<PropertyOccupancyDetailsEntity>> GetPropertyOccupancyByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyOccupancyDetails
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets RenterMast by PropertyDetailsId list.
    /// This entity only has PropertyDetailsId column (no PropertyId), so simple query is sufficient.
    /// </summary>
    public async Task<List<RenterMastEntity>> GetRentersByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.RenterMast
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region RoomWiseSubmissionDetails - Entity has BOTH PropertyId AND PropertyDetailsId (nullable)

    /// <summary>
    /// Gets all RoomWiseSubmissionDetails for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient to catch all records.
    /// Catches all records regardless of PropertyDetailsId state (NULL, valid, or orphaned).
    /// Use this method when deleting a property to ensure no orphaned records remain.
    /// </summary>
    public async Task<List<RoomWiseSubmissionDetailsEntity>> GetRoomWiseSubmissionByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.RoomWiseSubmissionDetails
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entities with ONLY PropertyId - BaseEntity only (no IHardDeletable)

    /// <summary>
    /// Gets PropertySocialDetails by PropertyId.
    /// This entity extends BaseEntity but does NOT implement IHardDeletable.
    /// Used for deactivation (IsActive=false) during property deletion.
    /// </summary>
    public async Task<List<PropertySocialDetailsEntity>> GetPropertySocialDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets WaterConnectionMaster by PropertyId.
    /// This entity extends BaseEntity but does NOT implement IHardDeletable.
    /// Used for deactivation (IsActive=false) during property deletion.
    /// </summary>
    public async Task<List<WaterConnectionMasterEntity>> GetWaterConnectionsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WaterConnectionMasterEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    // TODO: Uncomment when database table structure is finalized for PropertyTaxCalculationCVResultsEntity
    //public async Task<List<PropertyTaxCalculationCVResultsEntity>> GetCvResultsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    //{
    //    return await _context.PropertyTaxCalculationCVResults
    //        .AsNoTracking()
    //        .Where(x => propertyDetailIds.Contains(x.PropertyDetailsIds))
    //        .ToListAsync(cancellationToken);
    //}

    /// <summary>
    /// Gets RenterDetail entities by PropertyDetailsId list.
    /// This entity only has PropertyDetailsId column (no PropertyId), so simple query is sufficient.
    /// Used during property deletion to identify and mark all renter detail records.
    /// </summary>
    public async Task<List<RenterDetailEntity>> GetRenterDetailsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.RenterDetails
            .AsNoTracking()
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all related entities for a property that need to be marked for deletion.
    /// Returns entities implementing IHardDeletable.
    /// 
    /// NOTE: Queries are executed sequentially to avoid DbContext concurrency issues.
    /// EF Core's DbContext is not thread-safe and cannot handle parallel queries on the same instance.
    /// </summary>
    public async Task<List<IHardDeletable>> GetRelatedEntitiesForDeletionAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Build the list of related entities
        var relatedEntities = new List<IHardDeletable>();

        // Execute queries sequentially to avoid DbContext concurrency issues
        // Each query is independent but must run one at a time on the same DbContext

        var applyTaxes = await _context.ApplyTaxesMaster.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(applyTaxes);

        var flags = await _context.FlagMaster.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(flags);

        var plots = await _context.PlotDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(plots);

        var policyTax = await _context.PolicyTaxDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(policyTax);

        var assessmentDetails = await _context.PropertyAssessmentDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(assessmentDetails);

        var images = await _context.PropertyImagesMast.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(images);

        // Note: PropertySocialDetails and WaterConnectionMaster do not implement IHardDeletable.
        // These entities are now handled using DeactivatePropertyEntities() in PropertyService.MarkPropertyDetailsAndRelatedAsync().
        // They only get IsActive=false and UpdatedDate set, without MarkedForDeletion flags.

        // Note: PropertyTaxCalculationCVResultsEntity and RenterDetailEntity use PropertyDetailsId (not PropertyId).
        // They are handled in PropertyService.MarkPropertyDetailsAndRelatedAsync() method with TODO comments there.

        var taxPending = await _context.TaxPendingDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPending);

        var taxPendingArchive = await _context.TaxPendingDetailsArchive.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingArchive);

        var taxPendingCV = await _context.TaxPendingDetailsCV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingCV);

        var taxPendingLookup = await _context.TaxPendingDetailsLookup.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingLookup);

        var taxPendingRetro = await _context.TaxPendingDetailsRetro.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingRetro);

        var taxPendingRV = await _context.TaxPendingDetailsRV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingRV);

        var transMast = await _context.TransMast.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMast);

        var transMastArchive = await _context.TransMastArchive.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastArchive);

        var transMastLookup = await _context.TransMastLookup.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastLookup);

        var transMastRV = await _context.TransMastRV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastRV);

        var transMastCV = await _context.TransMastCV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastCV);

        // TODO: Uncomment when database table structure is finalized

        //var propertyCertificates = await _context.PropertyCertificates.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        //relatedEntities.AddRange(propertyCertificates);

        var propertyAssessments = await _context.PropertyMastDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(propertyAssessments);

        //var societyDetails = await _context.SocietyDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        //relatedEntities.AddRange(societyDetails);

        return relatedEntities;
    }

    /// <summary>
    /// Marks a collection of entities for soft deletion using the same logic as Repository.DeleteAsync.
    /// Sets MarkedForDeletion to true, MarkedForDeletionDate to current time (if not already set),
    /// IsActive to false, and UpdatedDate to current time for entities implementing BaseEntity.
    /// This method ensures consistency with the deletion logic in the base Repository class.
    /// </summary>
    /// <typeparam name="T">Entity type that implements IHardDeletable</typeparam>
    /// <param name="entities">The entities to mark for deletion</param>
    public void MarkEntitiesForDeletion<T>(IEnumerable<T> entities) where T : class, IHardDeletable
    {
        var deletionTime = DateTime.Now;

        foreach (var entity in entities)
        {
            // Set hard deletion flags
            entity.MarkedForDeletion = true;

            // Only set deletion date if not already set (preserves original deletion timestamp)
            if (!entity.MarkedForDeletionDate.HasValue)
            {
                entity.MarkedForDeletionDate = deletionTime;
            }

            // Set IsActive and UpdatedDate if the entity is a BaseEntity
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsActive = false;
                baseEntity.UpdatedDate = deletionTime;
            }

            // Mark entity as modified in EF Core
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
    /// <summary>
    /// Deactivates a collection of BaseEntity-derived entities by setting IsActive = false and UpdatedDate = now.
    /// Does NOT touch MarkedForDeletion or MarkedForDeletionDate.
    /// Used for entities that don't implement IHardDeletable (e.g., PropertySocialDetails, WaterConnectionMaster).
    /// </summary>
    /// <param name="entities">The entities to deactivate</param>
    public void DeactivatePropertyEntities(IEnumerable<BaseEntity> entities)
    {
        var now = DateTime.Now;
        foreach (var entity in entities)
        {
            entity.IsActive = false;
            entity.UpdatedDate = now;
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public async Task<CreateBulkPropertyResponseDto?> CreateBulkPropertyAsync(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default)
    {
        var propertyExists = await _context.PropertyMast.AnyAsync(x => x.WardId == dto.WardId && x.PropertyNo == dto.PropertyNo.Trim() && x.PartitionNo == dto.PartitionNo.Trim(), cancellationToken);

        if (propertyExists)
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = string.Join(" ", "PropertyNo already exists in our records.")
            };

        // Transaction
        PropertyEntity? property = null;
        PropertyAssessmentEntity? propertyMastDetails = null;
        try
        {
            var category = await _context.PropertyCategoryMaster.FirstOrDefaultAsync(x => x.Id == dto.CategoryId, cancellationToken);
            var MainPropertyDetails = await _context.PropertyMast.FirstOrDefaultAsync(x => x.WardId == dto.WardId && x.PropertyNo == dto.PropertyNo && x.PartitionNo == "", cancellationToken);
            // Validate category exists and is not "apartment" type
            if (category == null)
            {
                return new CreateBulkPropertyResponseDto
                {
                    Success = false,
                    Message = "Invalid CategoryId - category not found."
                };
            }

            if (category.PropertyCategoryName != null &&
                category.PropertyCategoryName.Contains("apartment", StringComparison.OrdinalIgnoreCase) && (dto.SocietyDetailId == null || dto.SocietyDetailId == 0))
            {
                return new CreateBulkPropertyResponseDto
                {
                    Success = false,
                    Message = "Society Wing Details is not Found"
                };
            }
            bool OpenPlot = false;

            if (category.PropertyCategoryName != null && category.PropertyCategoryName.Contains("plot", StringComparison.OrdinalIgnoreCase))
            {
                OpenPlot = true;
            }
            if (!string.IsNullOrEmpty(dto.PartitionNo) &&
                 dto.PartitionNo.Contains(PartitionNoConstants.AmenityPartitionNo, StringComparison.OrdinalIgnoreCase))
            {
                var propertyType = await _context.PropertyTypeMasters
                .FirstOrDefaultAsync(x => x.PartType == PartTypeConstants.Amenity, cancellationToken);

                if (propertyType == null)
                {
                    return new CreateBulkPropertyResponseDto
                    {
                        Success = false,
                        Message = "Amenity property type not found"
                    };
                }

                dto.PropertyTypeId = propertyType.Id;
            }
            // Property insert
            property = new PropertyEntity
            {
                TaxZoneId = dto.TaxZoneId,
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo.Trim(),
                PartitionNo = dto.PartitionNo.Trim(),
                PropertySeqNo = MainPropertyDetails?.PropertySeqNo,
                PropertyTypeId = dto.PropertyTypeId,
                CategoryId = dto.CategoryId,
                OwnerTitle = string.Empty,
                OwnerTitleEnglish = string.Empty,
                OpenPlot = OpenPlot,
                OwnerName = "धारक",
                OwnerNameEnglish = "The Holder",
                FlatOrShopNo = dto.FlatOrShopNo,
                FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish,
                Address = MainPropertyDetails?.Address,
                AddressEnglish = MainPropertyDetails?.AddressEnglish,
                Location = MainPropertyDetails?.Location,
                LocationEnglish = MainPropertyDetails?.LocationEnglish,
                SocietyDetailId = dto.SocietyDetailId,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto.CreatedBy
            };

            _context.PropertyMast.Add(property);
            var propertySaveResult = await _context.SaveChangesAsync(cancellationToken);

            // Assessment insert 
            propertyMastDetails = new PropertyAssessmentEntity
            {
                PropertyId = property.Id,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto.CreatedBy
            };

            _context.PropertyMastDetails.Add(propertyMastDetails);
            var assessmentSaveResult = await _context.SaveChangesAsync(cancellationToken);

            return new CreateBulkPropertyResponseDto
            {
                PropertyId = property.Id,
                Success = true,
                Message = "Property generated successfully."
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another user modified the same record mid-transaction
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = $"A concurrency conflict occurred. Please retry. detail: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            // Any unexpected error
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = $"An unexpected error occurred : {ex.Message}"
            };
        }
    }

}

