using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing inventory item condition operations.
/// Provides CRUD operations for inventory item conditions (e.g., New, Used, Damaged).
/// </summary>
public class InventoryItemConditionService : BaseCommonCrudService<InventoryItemConditionEntity, InventoryItemConditionDto, CreateInventoryItemConditionMasterDto, UpdateInventoryItemConditionMasterDto, InventoryItemConditionQueryParameters, int>,
    IInventoryItemConditionService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemConditionService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item condition entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    public InventoryItemConditionService(
        IRepository<InventoryItemConditionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
