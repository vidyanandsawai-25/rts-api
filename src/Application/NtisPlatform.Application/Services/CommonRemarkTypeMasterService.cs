using AutoMapper;
using NtisPlatform.Application.DTOs.Master.CommonRemarkTypeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class CommonRemarkTypeMasterService : BaseCommonCrudService<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos, CreateCommonRemarkTypeMasterDto, UpdateCommonRemarkTypeMasterDto, CommonRemarkTypeQueryParameters, int>, ICommonRemarkTypeMasterService
{
    public CommonRemarkTypeMasterService(
        IRepository<CommonRemarkTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
