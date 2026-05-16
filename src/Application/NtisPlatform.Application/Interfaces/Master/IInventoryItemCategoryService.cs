using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Service interface for inventory item category CRUD operations.
/// Inherits all common CRUD operations from <see cref="ICommonCrudService{TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey}"/>.
/// </summary>
public interface IInventoryItemCategoryService : ICommonCrudService<InventoryItemCategoryEntity, InventoryItemCategoryDto, CreateInventoryItemCategoryDto, UpdateInventoryItemCategoryDto, InventoryItemCategoryQueryParameters, int>
{
}
