using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for PropertyDetails CRUD operations
/// </summary>
public interface IDataEntryService : ICommonCrudService<PropertyDetailsEntity, PropertyDetailsDto, CreatePropertyDetailsDto, UpdatePropertyDetailsDto, PropertyDetailsQueryParameters, int>
{
    Task<PropertyDto?> UpdatePropertyAsync(int id, UpdatePropertyMastDto updateDto, CancellationToken cancellationToken = default);

    Task<PropertyDto?> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);
}
