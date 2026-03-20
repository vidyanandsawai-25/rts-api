using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class TypeOfUseService : BaseCommonCrudService<TypeOfUseEntity, TypeOfUseDto, CreateTypeOfUseDto, UpdateTypeOfUseDto, TypeOfUseQueryParameters, int>, ITypeOfUseService
{
    public TypeOfUseService(
        IRepository<TypeOfUseEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

}

