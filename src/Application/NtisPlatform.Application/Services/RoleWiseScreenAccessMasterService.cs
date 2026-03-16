using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RoleWiseScreenAccessMasterService : BaseCommonCrudService<RoleWiseScreenAccessMasterEntity, RoleWiseScreenAccessMasterDTO, CreateRoleWiseScreenAccessMasterDto, UpdateRoleWiseScreenAccessMasterDto, RoleWiseScreenAccessQueryParameters, int>, IRoleWiseScreenAccessMasterService
    {
        public RoleWiseScreenAccessMasterService(
            IRepository<RoleWiseScreenAccessMasterEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }

        /// <summary>
        /// Creates a new role-wise screen access with duplicate validation
        /// </summary>
        public override async Task<RoleWiseScreenAccessMasterDTO> CreateAsync(
            CreateRoleWiseScreenAccessMasterDto createDto,
            CancellationToken cancellationToken = default)
        {
            // Check for duplicate (UserRoleId, ScreenId) combination
            var exists = await _repository.GetQueryable()
                .AnyAsync(x => x.UserRoleId == createDto.UserRoleId
                            && x.ScreenId == createDto.ScreenId
                            && x.IsActive,
                          cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    $"Role-Screen access already exists for UserRoleId={createDto.UserRoleId} and ScreenId={createDto.ScreenId}");
            }

            return await base.CreateAsync(createDto, cancellationToken);
        }

        /// <summary>
        /// Updates role-wise screen access with duplicate validation
        /// </summary>
        public override async Task<RoleWiseScreenAccessMasterDTO?> UpdateAsync(
            int id,
            UpdateRoleWiseScreenAccessMasterDto updateDto,
            CancellationToken cancellationToken = default)
        {
            // Check for duplicate with different id
            var exists = await _repository.GetQueryable()
                .AnyAsync(x => x.RoleWiseScreenAccessId != id
                            && x.UserRoleId == updateDto.UserRoleId
                            && x.ScreenId == updateDto.ScreenId
                            && x.IsActive,
                          cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    $"Role-Screen access already exists for UserRoleId={updateDto.UserRoleId} and ScreenId={updateDto.ScreenId}");
            }

            return await base.UpdateAsync(id, updateDto, cancellationToken);
        }
    }
}
