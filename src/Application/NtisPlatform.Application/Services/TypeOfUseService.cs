using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class TypeOfUseService : BaseCommonCrudService<TypeOfUseEntity, TypeOfUseDto, CreateTypeOfUseDto, UpdateTypeOfUseDto, TypeOfUseQueryParameters, string>, ITypeOfUseService
{
    public TypeOfUseService(
        IRepository<TypeOfUseEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

}

