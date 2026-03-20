using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Application.Services;

public class TypeOfUseGroupService : BaseCommonCrudService<TypeOfUseGroupEntity, TypeOfUseGroupDto, CreateTypeOfUseGroupDto, UpdateTypeOfUseGroupDto, TypeOfUseGroupQueryParameters, int>, ITypeOfUseGroupService
{
    public TypeOfUseGroupService(
    IRepository<TypeOfUseGroupEntity, int> repository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : base(repository, unitOfWork, mapper)
    {
    }
}

