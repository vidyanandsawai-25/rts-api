using NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.Property;

public interface IPropertyWorkflowDetailsService
    : ICommonCrudService<PropertyWorkflowDetailsEntity, PropertyWorkflowDetailsDto, CreatePropertyWorkflowDetailsDto, UpdatePropertyWorkflowDetailsDto, PropertyWorkflowDetailsQueryParameters, int>
{
    Task<List<PropertyWorkflowDetailsDto>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyWorkflowDetailsDto?> GetCurrentByPropertyNoAsync(string propertyid, CancellationToken cancellationToken = default);
}
