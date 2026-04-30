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
    /// Updates old property details across PropertyMastOld and PropertyDetailsOld tables
    /// <returns>Updated PropertyBasicDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves KYC details for a property including joined data from related tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property KYC details DTO or null if not found</returns>
    Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <returns>Updated PropertyKycDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default);

    /// Updates society details for a property
    /// <returns>Updated PropertyBasicDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
        
    /// <returns>Updated PropertySocietyDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves old property details including joined data from PropertyMastOld and PropertyDetailsOld tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property old details DTO or null if not found</returns>
    Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tax details for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property tax details DTO or null if not found</returns>
    Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tax details CV for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property tax details CV DTO or null if not found</returns>
    Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default);
    /// Retrieves old taxes details for a property including historical tax data across finance years
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property old taxes details DTO or null if property not found</returns>
    Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates old taxes details for a property across multiple finance years
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update data containing tax information for multiple years</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertyOldTaxesDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all historical floor details for a property (PropertyDetailsOld records)
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of historical floor details or null if property not found</returns>
    Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates historical floor details for a property (batch create, update, delete)
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update data containing floor detail records to create, update, or delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertyDetailsOldListDto if property was found and updated, null otherwise</returns>
    Task<PropertyDetailsOldListDto?> UpdateFloorDetailsOldAsync(int propertyId, UpdatePropertyDetailsOldListDto dto, CancellationToken cancellationToken = default);
}
