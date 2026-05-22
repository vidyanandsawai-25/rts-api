using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwnershipTypeService : BaseCommonCrudService<
    OwnershipTypeEntity,
    OwnershipTypeDto,
    CreateOwnershipTypeDto,
    UpdateOwnershipTypeDto,
    OwnershipTypeQueryParameters,
    int>, IOwnershipTypeService
{
    public OwnershipTypeService(
        IRepository<OwnershipTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
