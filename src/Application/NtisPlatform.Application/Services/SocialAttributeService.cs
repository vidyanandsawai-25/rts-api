using AutoMapper;
using NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class SocialAttributeService : BaseCommonCrudService<SocialAttributeEntity, SocialAttributeDto, CreateSocialAttributeDto, UpdateSocialAttributeDto, SocialAttributeMasterQueryParameters, int>, ISocialAttributeService
    {
        public SocialAttributeService(IRepository<SocialAttributeEntity, int> repository,IUnitOfWork unitOfWork, IMapper mapper)
        : base(repository, unitOfWork, mapper)
        {
            
        }
    }
}