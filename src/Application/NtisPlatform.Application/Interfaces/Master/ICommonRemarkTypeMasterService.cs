using NtisPlatform.Application.DTOs.Master.CommonRemarkTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface ICommonRemarkTypeMasterService : ICommonCrudService<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos, CreateCommonRemarkTypeMasterDto, UpdateCommonRemarkTypeMasterDto, CommonRemarkTypeQueryParameters, int>
{
}
