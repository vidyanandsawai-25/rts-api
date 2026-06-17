using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>Data-access implementation for the Property "KYC Details" tab (queries and staged inserts only).</summary>
public class PropertyKycRepository : PropertyRepositoryBase, IPropertyKycRepository
{
    public PropertyKycRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Project only the KYC columns needed — avoid loading the full entity for a read-only query.
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new
            {
                p.Id,
                p.OwnerTitle, p.OwnerName, p.OwnerTitleEnglish, p.OwnerNameEnglish,
                p.OccupierTitle, p.OccupierName, p.OccupierTitleEnglish, p.OccupierNameEnglish,
                p.Address, p.Location, p.AddressEnglish, p.LocationEnglish,
                p.FlatOrShopName, p.FlatOrShopNameEnglish, p.FlatOrShopNo, p.FlatOrShopNoEnglish,
                p.MobileNo, p.EmailId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Get PropertyMastDetails (assessment) with OwnerTypeId and AdharCardNo.
        var assessment = await _context.PropertyMastDetails
            .AsNoTracking()
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

        // Step 3: Resolve OwnerType name — project only the single column needed.
        string? ownerType = null;
        if (assessment?.OwnerTypeId.HasValue == true)
        {
            ownerType = await _context.OwnerTypeMaster
                .AsNoTracking()
                .Where(x => x.Id == assessment.OwnerTypeId.Value && x.IsActive)
                .Select(x => x.OwnerType)
                .FirstOrDefaultAsync(cancellationToken);
        }

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
}
