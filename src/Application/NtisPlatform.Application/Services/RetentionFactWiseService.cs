using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RetentionFactWiseService : BaseCommonCrudService<RetentionFactWiseEntity, RetentionFactWiseDto, CreateRetentionFactWiseDto, UpdateRetentionFactWiseDto, RetentionFactWiseQueryParameters, int>, IRetentionFactWiseService
    {
        public RetentionFactWiseService(IRepository<RetentionFactWiseEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
