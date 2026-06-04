using AutoMapper;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class BulkUpdateMasterService : BaseCommonCrudService<BulkUpdateMasterEntity, BulkUpdateMasterDto, CreateBulkUpdateMasterDto, UpdateBulkUpdateMasterDto, BulkUpdateMasterQueryParameters, int>, IBulkUpdateMasterService
{
    public BulkUpdateMasterService(
        IRepository<BulkUpdateMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
