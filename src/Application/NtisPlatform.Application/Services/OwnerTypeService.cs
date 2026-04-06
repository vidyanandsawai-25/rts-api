using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwnerTypeService : BaseCommonCrudService<OwnerTypeMasterEntity, OwnerTypeDto, CreateOwnerTypeDto, UpdateOwnerTypeDto, OwnerTypeQueryParameters, int>, IOwnerTypeService
{
    public OwnerTypeService(
        IRepository<OwnerTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}