using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Application.Services;

public class TypeOfUseGroupService : BaseCommonCrudService<TypeOfUseGroupEntity, TypeOfUseGroupDto, CreateTypeOfUseGroupDto, UpdateTypeOfUseGroupDto, TypeOfUseGroupQueryParameters, string>, ITypeOfUseGroupService
{
    public TypeOfUseGroupService(
    IRepository<TypeOfUseGroupEntity, string> repository,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : base(repository, unitOfWork, mapper)
    {
    }
}

