using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Application.Interfaces;

public interface IConstructionTypeService : ICommonCrudService<ConstructionTypeEntity, ConstructionTypeDto, CreateConstructionTypeDto, UpdateConstructionTypeDto, ConstructionTypeQueryParameters, string>
{
    Task<PagedResult<ConstructionTypeDto>> GetAllWithHierarchyAsync(
        ConstructionTypeQueryParameters queryParameters,
        string hierarchyFilter,
        CancellationToken cancellationToken = default);
}
