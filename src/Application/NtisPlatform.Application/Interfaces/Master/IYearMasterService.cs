using NtisPlatform.Application.DTOs.Master.YearMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IYearMasterService : ICommonCrudService<YearMasterEntity, YearMasterDto, CreateYearMasterDto, UpdateYearMasterDto, YearMasterQueryParameters, int>
    {
    }
}
