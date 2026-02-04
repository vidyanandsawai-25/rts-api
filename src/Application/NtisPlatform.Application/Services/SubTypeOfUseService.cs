using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;


namespace NtisPlatform.Application.Services;

public class SubTypeOfUseService : BaseCommonCrudService<SubTypeOfUseEntity, SubTypeOfUseDto, CreateSubTypeOfUseDto, UpdateSubTypeOfUseDto, SubTypeOfUseQueryParameters, int>, ISubTypeOfUseService
{
    public SubTypeOfUseService(
         IRepository<SubTypeOfUseEntity, int> repository,
         IUnitOfWork unitOfWork,
         IMapper mapper)
         : base(repository, unitOfWork, mapper)
    {
    }
}

