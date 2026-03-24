using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwnerTypeService : BaseCommonCrudService<OwnerTypeEntity, OwnerTypeDto, CreateOwnerTypeDto, UpdateOwnerTypeDto, OwnerTypeQueryParameters, int>, IOwnerTypeService
{
    public OwnerTypeService(
        IRepository<OwnerTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}