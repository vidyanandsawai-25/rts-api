using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class UserMasterMappingProfile : Profile
{
    public UserMasterMappingProfile()
    {
        // Base
        CreateMap<BaseEntity, BaseDtos>();

        // Entity -> UserDto (GET endpoints)
        CreateMap<UserEntity, UserDto>()
            .IncludeBase<BaseEntity, BaseDtos>()
            .ForMember(dest => dest.Departments, opt => opt.Ignore())
            .ForMember(dest => dest.ModuleAccess, opt => opt.Ignore())
            .ForMember(dest => dest.RoleAllocations, opt => opt.Ignore());

        // Entity -> UserSecurityStatusDto (activate / deactivate / reset-password responses)
        CreateMap<UserEntity, UserSecurityStatusDto>();

        // CreateUserDto -> Entity
        CreateMap<CreateUserDto, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.MustChangePassword, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
            .ForMember(dest => dest.LockedUntilAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // UpdateUserDto -> Entity
        // MustChangePassword is NOT included in UpdateUserDto and explicitly ignored here
        // to prevent clients from bypassing the forced password change flow.
        CreateMap<UpdateUserDto, UserEntity>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.MustChangePassword, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
            .ForMember(dest => dest.LockedUntilAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

        // DeactivateUserDto -> Entity: only IsActive = false, everything else ignored
        CreateMap<DeactivateUserDto, UserEntity>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.Ignore())
            .ForMember(dest => dest.MiddleName, opt => opt.Ignore())
            .ForMember(dest => dest.LastName, opt => opt.Ignore())
            .ForMember(dest => dest.UserCode, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.MobileNo, opt => opt.Ignore())
            .ForMember(dest => dest.AlternateMobileNo, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.MustChangePassword, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeTypeID, opt => opt.Ignore())
            .ForMember(dest => dest.Language, opt => opt.Ignore())
            .ForMember(dest => dest.Remark, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
            .ForMember(dest => dest.LockedUntilAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        // ActivateUserDto -> Entity: only IsActive = true, everything else ignored
        CreateMap<ActivateUserDto, UserEntity>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.Ignore())
            .ForMember(dest => dest.MiddleName, opt => opt.Ignore())
            .ForMember(dest => dest.LastName, opt => opt.Ignore())
            .ForMember(dest => dest.UserCode, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.MobileNo, opt => opt.Ignore())
            .ForMember(dest => dest.AlternateMobileNo, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.MustChangePassword, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeTypeID, opt => opt.Ignore())
            .ForMember(dest => dest.Language, opt => opt.Ignore())
            .ForMember(dest => dest.Remark, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
            .ForMember(dest => dest.LockedUntilAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
    }
}