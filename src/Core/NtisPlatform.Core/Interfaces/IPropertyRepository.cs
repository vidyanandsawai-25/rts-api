using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Specialized repository interface for Property-specific queries
/// Extends the generic repository with custom query methods
/// </summary>
public interface IPropertyRepository : IRepository<PropertyEntity, int>
{
    /// <summary>
    /// Retrieves basic details for a property including joined data from related tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property basic details DTO or null if not found</returns>
    Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates society details for a property
    /// <returns>Updated PropertyBasicDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves society details for a property including joined data from related tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property society details DTO or null if not found</returns>
    Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates society details for a property across multiple tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertySocietyDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default);


    /// <summary>
    /// Retrieves KYC details for a property including joined data from related tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property KYC details DTO or null if not found</returns>
    Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <returns>Updated PropertyKycDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default);

}
