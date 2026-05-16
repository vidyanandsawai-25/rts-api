using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing inventory item model operations.
/// Provides CRUD operations for inventory item models.
/// </summary>
public class InventoryItemModelService : BaseCommonCrudService<InventoryItemModelEntity, InventoryItemModelDto, CreateInventoryItemModelDto, UpdateInventoryItemModelDto, InventoryItemModelQueryParameters, int>,
  IInventoryItemModelService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemModelService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item model entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    public InventoryItemModelService(
        IRepository<InventoryItemModelEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
