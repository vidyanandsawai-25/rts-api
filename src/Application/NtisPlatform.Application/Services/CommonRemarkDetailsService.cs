using AutoMapper;
using NtisPlatform.Application.DTOs.Master.CommonRemarkDetails;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class CommonRemarkDetailsService : BaseCommonCrudService<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos, CreateCommonRemarkDetailsDto, UpdateCommonRemarkDetailsDto, CommonRemarkDetailsQueryParameters, int>, ICommonRemarkDetailsService
    {
        public CommonRemarkDetailsService(
            IRepository<CommonRemarkDetailsEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }
    }
}
