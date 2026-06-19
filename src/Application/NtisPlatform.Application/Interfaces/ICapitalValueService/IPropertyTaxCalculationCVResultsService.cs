using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService;

public interface IPropertyTaxCalculationCVResultsService : ICommonCrudService<PropertyTaxCalculationCVResultsEntity, PropertyTaxCalculationCVResultsDto, CreatePropertyTaxCalculationCVResultsDto, UpdatePropertyTaxCalculationCVResultsDto, PropertyTaxCalculationCVResultsQueryParameters, long>
{
    Task<List<PropertyTaxCalculationCVResultsDto>> GetByPropertyIdAsync(long propertyId, CancellationToken cancellationToken = default);
    Task<List<PropertyTaxCalculationCVResultsDto>> GetByPropertyDetailsIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all CV records for a specific PropertyDetailsId.
    /// Used when property details have changed (detected via hash) and recalculation is needed.
    /// Sets IsActive = false (soft delete).
    /// </summary>
    Task DeactivateByPropertyDetailsIdAsync(int propertyDetailsId, int? updatedBy = null, CancellationToken cancellationToken = default);
    Task DeactivateByPropertyIdAsync(int propertyId, int? updatedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the CVInputHash for a PropertyDetailsId to detect changes.
    /// Returns null if no CV records exist for this PropertyDetailsId.
    /// </summary>
    Task<string?> GetCVInputHashAsync(int propertyDetailsId, CancellationToken cancellationToken = default);
}
