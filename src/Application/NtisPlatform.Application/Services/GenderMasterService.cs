using AutoMapper;
using NtisPlatform.Application.DTOs.Master.GenderMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GenderMasterService : BaseCommonCrudService<GenderMasterEntity, GenderMasterDtos, CreateGenderMasterDto, UpdateGenderMasterDto, GenderQueryParameters, int>, IGenderMasterService
{
    public GenderMasterService(
        IRepository<GenderMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}