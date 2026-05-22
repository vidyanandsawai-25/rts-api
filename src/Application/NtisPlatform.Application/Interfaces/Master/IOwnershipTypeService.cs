using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IOwnershipTypeService : ICommonCrudService<
        OwnershipTypeEntity,
        OwnershipTypeDto,
        CreateOwnershipTypeDto,
        UpdateOwnershipTypeDto,
        OwnershipTypeQueryParameters,
        int>
    {
    }
}
