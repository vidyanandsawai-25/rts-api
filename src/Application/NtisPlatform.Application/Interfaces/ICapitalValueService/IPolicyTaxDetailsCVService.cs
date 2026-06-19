using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService;

public interface IPolicyTaxDetailsCVService : ICommonCrudService<PolicyTaxDetailsCVEntity, PolicyTaxDetailsCVDto, CreatePolicyTaxDetailsCVDto, UpdatePolicyTaxDetailsCVDto, PolicyTaxDetailsCVQueryParameters, int>
{
    Task<PolicyTaxDetailsCVDto?> GetByPropertyAndTaxIdAsync(long propertyId, int taxId, CancellationToken cancellationToken = default);
    Task<List<PolicyTaxDetailsCVDto>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes all PolicyTaxDetails records for a property by setting IsActive=false and MarkedForDeletion=true.
    /// Used when CV recalculation is triggered due to property details changes.
    /// </summary>
    Task DeactivateByPropertyIdAsync(int propertyId,  int? updatedBy = null, CancellationToken cancellationToken = default);


}
