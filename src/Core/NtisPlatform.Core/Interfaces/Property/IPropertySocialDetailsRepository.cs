using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Data access for the Property "Social Info" tab. Encapsulates the social-attribute and
/// property-social-detail queries so the application service contains no EF Core query expressions.
/// </summary>
public interface IPropertySocialDetailsRepository
{
    /// <summary>Returns all active social attributes ordered by display order then code (the full hierarchy source).</summary>
    Task<List<SocialAttributeEntity>> GetActiveSocialAttributesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the property's active social-detail rows as tracked entities (for hierarchy values and upsert).</summary>
    Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsByPropertyAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the property's active social-detail rows with their SocialAttribute loaded (for the upsert response projection).</summary>
    Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsWithAttributeByPropertyAsync(int propertyId, CancellationToken cancellationToken = default);
}
