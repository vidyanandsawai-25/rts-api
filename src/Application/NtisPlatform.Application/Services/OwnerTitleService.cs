using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwnerTitleService : BaseCommonCrudService<OwnerTitleMasterEntity, OwnerTitleDto, CreateOwnerTitleDto, UpdateOwnerTitleDto, OwnerTitleQueryParameters, int>, IOwnerTitleService
{
    public OwnerTitleService(
        IRepository<OwnerTitleMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
