using NtisPlatform.Application.DTOs.Master.UserRoleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IUserRoleService : ICommonCrudService<UserRoleMasterEntity, UserRoleMasterDto, CreateUserRoleMasterDto, UpdateUserRoleMasterDto, UserRoleMasterQueryParameterDto, int>
{
}
