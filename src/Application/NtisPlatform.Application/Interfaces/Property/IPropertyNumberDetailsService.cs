using NtisPlatform.Application.DTOs.PropertyNumberDetails;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyNumberDetailsService
{
    Task<PropertyNumberDetailsDto?> GetAsync(int propertyId,string filter, CancellationToken cancellationToken = default);
}
