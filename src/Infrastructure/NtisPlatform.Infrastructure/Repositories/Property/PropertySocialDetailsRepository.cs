using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>Data-access implementation for the Property "Social Info" tab (queries only).</summary>
public class PropertySocialDetailsRepository : IPropertySocialDetailsRepository
{
    private readonly ApplicationDbContext _context;

    public PropertySocialDetailsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SocialAttributeEntity>> GetActiveSocialAttributesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SocialAttributeEntity>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
            .ThenBy(x => x.SocialAttributeCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsByPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsWithAttributeByPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Include(x => x.SocialAttribute)
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .ToListAsync(cancellationToken);
    }
}
