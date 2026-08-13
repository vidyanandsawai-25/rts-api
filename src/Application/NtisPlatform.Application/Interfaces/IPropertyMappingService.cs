using NtisPlatform.Application.DTOs.PropertyMapDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

public interface IPropertyMappingService : ICommonCrudService<PropertyMapDetailEntity, PropertyMapDetailDto, CreatePropertyMapDetailsDto, UpdatePropertyMapDetailsDto, PropertyMapDetailsQueryParameters, int>
{
    Task<List<PropertyMatchingResponseDto>> GetPropertyMatchingDetailsAsync(PropertyMapDetailsQueryParameters request, CancellationToken cancellationToken = default);
}
