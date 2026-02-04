using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RetentionYearWiseService : BaseCommonCrudService<RetentionYearWiseEntity, RetentionYearWiseDto, CreateRetentionYearWiseDto, UpdateRetentionYearWiseDto, RetentionYearWiseQueryParameters, int>, IRetentionYearWiseService
    {
        public RetentionYearWiseService(IRepository<RetentionYearWiseEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
