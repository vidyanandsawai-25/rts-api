using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class UlbImageMasterService
    : BaseCommonCrudService<UlbImageMasterEntity, UlbImageMasterDto, CreateUlbImageMasterDto, UpdateUlbImageMasterDto, UlbImageMasterQueryParameters, int>,
      IUlbImageMasterService
{
    public UlbImageMasterService(
        IRepository<UlbImageMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
