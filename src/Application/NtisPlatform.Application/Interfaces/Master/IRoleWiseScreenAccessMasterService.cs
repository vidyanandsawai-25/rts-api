using NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IRoleWiseScreenAccessMasterService : ICommonCrudService<RoleWiseScreenAccessMasterEntity, RoleWiseScreenAccessMasterDTO, CreateRoleWiseScreenAccessMasterDto, UpdateRoleWiseScreenAccessMasterDto, RoleWiseScreenAccessQueryParameters, int>
    {
    }
}
