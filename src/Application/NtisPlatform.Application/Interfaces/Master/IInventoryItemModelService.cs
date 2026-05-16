using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Service interface for inventory item model CRUD operations.
/// Inherits all common CRUD operations from <see cref="ICommonCrudService{TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey}"/>.
/// </summary>
public interface IInventoryItemModelService :
    ICommonCrudService<InventoryItemModelEntity,
  InventoryItemModelDto,
  CreateInventoryItemModelDto,
      UpdateInventoryItemModelDto,
        InventoryItemModelQueryParameters,
     int>
{
}
