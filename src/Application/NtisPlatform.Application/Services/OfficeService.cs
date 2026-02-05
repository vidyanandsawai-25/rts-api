using AutoMapper;
using NtisPlatform.Application.DTOs.Master.OfficeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class OfficeService : BaseCommonCrudService<OfficeEntity, OfficeDto, CreateOfficeDto, UpdateOfficeDto, OfficeQueryParameters, int>, IOfficeService
    {
        public OfficeService(
            IRepository<OfficeEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }
    }
}
