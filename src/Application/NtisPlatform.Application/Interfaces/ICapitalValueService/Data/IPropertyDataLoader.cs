using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;

/// <summary>
/// Abstraction for loading property and related data.
/// Decouples service from direct repository access.
/// </summary>
public interface IPropertyDataLoader
{
    /// <summary>
    /// Loads property entity with validation.
    /// </summary>
    Task<PropertyEntity> LoadPropertyAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads property details with all required navigation properties.
    /// </summary>
    Task<List<PropertyDetailsEntity>> LoadPropertyDetailsAsync(
        int propertyId, 
        int? propertyDetailsId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if property has active details.
    /// </summary>
    Task<bool> HasActiveDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads lift flag for property.
    /// </summary>
    Task<bool> LoadLiftFlagAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads active finance year.
    /// </summary>
    Task<YearMasterEntity> LoadFinanceYearAsync(int? specificYear = null, CancellationToken cancellationToken = default);
}
