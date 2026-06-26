using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

public interface IPropertyWorkflowDetailsRepository : IRepository<PropertyWorkflowDetailsEntity, int>
{
    Task ResetCurrentStatusAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<List<PropertyWorkflowDetailsEntity>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyWorkflowDetailsEntity?> GetCurrentByPropertyNoAsync(string propertyid, CancellationToken cancellationToken = default);
}
