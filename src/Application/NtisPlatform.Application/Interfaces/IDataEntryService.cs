using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for PropertyDetails CRUD operations
/// </summary>
public interface IDataEntryService : ICommonCrudService<PropertyDetailsEntity, PropertyDetailsDto, CreatePropertyDetailsDto, UpdatePropertyDetailsDto, PropertyDetailsQueryParameters, int>
{
}
