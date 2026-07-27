using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

public interface IInventoryDocumentTypeService : ICommonCrudService<InventoryDocumentTypeEntity, InventoryDocumentTypeDto, CreateInventoryDocumentTypeDto, UpdateInventoryDocumentTypeDto, InventoryDocumentTypeQueryParameters, int>
{
}
