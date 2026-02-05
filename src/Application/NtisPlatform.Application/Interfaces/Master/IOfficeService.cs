using NtisPlatform.Application.DTOs.Master.OfficeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IOfficeService : ICommonCrudService<OfficeEntity, OfficeDto, CreateOfficeDto, UpdateOfficeDto, OfficeQueryParameters, int>
    {
    }
}
