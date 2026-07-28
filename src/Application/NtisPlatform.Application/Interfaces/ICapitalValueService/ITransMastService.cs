using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService;

public interface ITransMastService : ICommonCrudService<TransMastEntity, TransMastDto, CreateTransMastDto, UpdateTransMastDto, TransMastQueryParameters, int>
{
    Task<TransMastDto?> GetByPropertyFinanceYearAndTaxIdAsync(long propertyId, string CalculationType, int financeYearId, int taxId, CancellationToken cancellationToken = default);
    Task<List<TransMastDto>> GetByPropertyIdAsync(int propertyId, string CalculationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes all TransMast records for a property by setting IsActive=false and MarkedForDeletion=true.
    /// Used when CV recalculation is triggered due to property details changes.
    /// </summary>
    Task DeactivateByPropertyIdAsync(int propertyId, string CalculationType, int? updatedBy = null, CancellationToken cancellationToken = default);
}
