using AutoMapper;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class BulkUpdateFieldConfigService : BaseCommonCrudService<BulkUpdateFieldConfigEntity, BulkUpdateFieldConfigDto, CreateBulkUpdateFieldConfigDto, UpdateBulkUpdateFieldConfigDto, BulkUpdateFieldConfigQueryParameters, int>, IBulkUpdateFieldConfigService
{
    public BulkUpdateFieldConfigService(
        IRepository<BulkUpdateFieldConfigEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
