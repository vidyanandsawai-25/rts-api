using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Service interface for inventory item condition CRUD operations.
/// Inherits all common CRUD operations from <see cref="ICommonCrudService{TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey}"/>.
/// </summary>
public interface IInventoryItemConditionService :
    ICommonCrudService<InventoryItemConditionEntity,
    InventoryItemConditionDto,
    CreateInventoryItemConditionMasterDto,
    UpdateInventoryItemConditionMasterDto,
    InventoryItemConditionQueryParameters,
  int>
{
}
