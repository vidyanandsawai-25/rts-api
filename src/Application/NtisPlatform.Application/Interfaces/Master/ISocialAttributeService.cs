using NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface ISocialAttributeService : ICommonCrudService<SocialAttributeEntity, SocialAttributeDto,CreateSocialAttributeDto,UpdateSocialAttributeDto, SocialAttributeMasterQueryParameters, int>
    {
    }
}
