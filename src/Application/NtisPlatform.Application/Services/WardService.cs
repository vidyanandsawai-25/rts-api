using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WardService : BaseCommonCrudService<WardEntity, WardDto, CreateWardDto, UpdateWardDto, WardQueryParameters, string>, IWardService
{
    public WardService(
        IRepository<WardEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}

