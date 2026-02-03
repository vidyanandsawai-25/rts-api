using AutoMapper;
using NtisPlatform.Application.DTOs.Master.YearMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class YearMasterService : BaseCommonCrudService<YearMasterEntity, YearMasterDto, CreateYearMasterDto, UpdateYearMasterDto, YearMasterQueryParameters, int>, IYearMasterService
    {
        public YearMasterService(
            IRepository<YearMasterEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }
    }
}
