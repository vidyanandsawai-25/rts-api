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
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertySocialDetailsEntity>> GetSocialDetailsByPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsWithAttributeByPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Include(x => x.SocialAttribute)
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertySocialDetailsEntity>> GetSocialDetailsByFiltersAsync(
        int? socialAttributeId,
        int? societyDetailId,
        int? wingDetailId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.Set<PropertySocialDetailsEntity>()
            .AsNoTracking()
            .Include(x => x.SocialAttribute)
            .Where(x => x.IsActive
                && !x.MarkedForDeletion
                && x.SocialAttribute != null
                && x.SocialAttribute.IsActive);

        var directQuery = baseQuery;

        if (socialAttributeId.HasValue)
        {
            directQuery = directQuery.Where(x => x.SocialAttributeId == socialAttributeId.Value);
        }

        if (wingDetailId.HasValue)
        {
            directQuery = directQuery.Where(x => x.WingDetailId == wingDetailId.Value
                && x.WingDetailsMast != null
                && x.WingDetailsMast.IsActive
                && !x.WingDetailsMast.MarkedForDeletion);
        }

        if (societyDetailId.HasValue)
        {
            directQuery = directQuery.Where(x => x.SocietyDetailId == societyDetailId.Value
                && x.SocietyDetailsMast != null
                && x.SocietyDetailsMast.IsActive
                && !x.SocietyDetailsMast.MarkedForDeletion);
        }

        var directMatches = await directQuery
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (directMatches.Count > 0 || (!wingDetailId.HasValue && !societyDetailId.HasValue))
        {
            return directMatches;
        }

        var hierarchyQuery = baseQuery;

        if (socialAttributeId.HasValue)
        {
            hierarchyQuery = hierarchyQuery.Where(x => x.SocialAttributeId == socialAttributeId.Value);
        }

        if (wingDetailId.HasValue)
        {
            var requestedWingDetailId = wingDetailId.Value;

            hierarchyQuery = hierarchyQuery.Where(x =>
                (x.WingDetailId == requestedWingDetailId
                    && x.WingDetailsMast != null
                    && x.WingDetailsMast.IsActive
                    && !x.WingDetailsMast.MarkedForDeletion) ||
                (x.PropertyMast != null
                    && x.PropertyMast.IsActive
                    && !x.PropertyMast.MarkedForDeletion
                    && x.PropertyMast.WingDetailId == requestedWingDetailId));
        }

        if (societyDetailId.HasValue)
        {
            var requestedSocietyDetailId = societyDetailId.Value;
            var societyWingDetailIds = _context.Set<WingDetailsMastEntity>()
                .AsNoTracking()
                .Where(w => w.SocietyDetailsMastId == requestedSocietyDetailId
                    && w.IsActive
                    && !w.MarkedForDeletion)
                .Select(w => w.Id);

            hierarchyQuery = hierarchyQuery.Where(x =>
                (x.SocietyDetailId == requestedSocietyDetailId
                    && x.SocietyDetailsMast != null
                    && x.SocietyDetailsMast.IsActive
                    && !x.SocietyDetailsMast.MarkedForDeletion) ||
                (x.WingDetailId.HasValue && societyWingDetailIds.Contains(x.WingDetailId.Value)) ||
                (x.PropertyMast != null
                    && x.PropertyMast.IsActive
                    && !x.PropertyMast.MarkedForDeletion
                    && x.PropertyMast.WingDetailId.HasValue
                    && societyWingDetailIds.Contains(x.PropertyMast.WingDetailId.Value)) ||
                _context.Set<SocietyDetailsEntity>().Any(s =>
                    s.Id == requestedSocietyDetailId
                    && s.PropertyId == x.PropertyId
                    && s.IsActive
                    && !s.MarkedForDeletion));
        }

        return await hierarchyQuery
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    }
}
