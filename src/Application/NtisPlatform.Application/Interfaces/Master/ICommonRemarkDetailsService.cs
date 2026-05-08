using NtisPlatform.Application.DTOs.Master.CommonRemarkDetails;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface ICommonRemarkDetailsService : ICommonCrudService<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos, CreateCommonRemarkDetailsDto, UpdateCommonRemarkDetailsDto, CommonRemarkDetailsQueryParameters, int>
    {
    }
}
