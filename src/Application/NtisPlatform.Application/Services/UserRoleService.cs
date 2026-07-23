using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.UserRoleMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Linq;

namespace NtisPlatform.Application.Services
{
    public class UserRoleService : BaseCommonCrudService<UserRoleMasterEntity, UserRoleMasterDto, CreateUserRoleMasterDto, UpdateUserRoleMasterDto, UserRoleMasterQueryParameterDto, int>, IUserRoleService
    {
        public UserRoleService(IRepository<UserRoleMasterEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }

        protected override IQueryable<UserRoleMasterEntity> ApplyIncludes(IQueryable<UserRoleMasterEntity> query)
        {
            return query.Include(x => x.Department);
        }
    }
}
