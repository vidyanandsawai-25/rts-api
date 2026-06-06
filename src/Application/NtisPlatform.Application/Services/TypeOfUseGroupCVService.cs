using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class TypeOfUseGroupCVService : BaseCommonCrudService<TypeOfUseGroupCVEntity, TypeOfUseGroupCVDto, CreateTypeOfUseGroupCVDto, UpdateTypeOfUseGroupCVDto, TypeOfUseGroupCVQueryParameters, int>, ITypeOfUseGroupCVService
{
    public TypeOfUseGroupCVService(
        IRepository<TypeOfUseGroupCVEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
