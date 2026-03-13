using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.UserRoleMaster
{
    public class UserRoleMasterDto : BaseDtos
    {
        public int UserRoleId { get; set; } = 0;
        public string UserRoleName { get; set; } = string.Empty;
    }

    public class CreateUserRoleMasterDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "UserRoleName_Required")]
        [StringLength(100, ErrorMessage = "UserRoleName_MaxLen_100")]
        public string UserRoleName { get; set; } = string.Empty;
    }

    public class UpdateUserRoleMasterDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "UserRoleName_Required")]
        [StringLength(100, ErrorMessage = "UserRoleName_MaxLen_100")]
        public string UserRoleName { get; set; } = string.Empty;
    }
    
    public class UserRoleMasterQueryParameterDto : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        public string? UserRoleName { get; set; }
        [Filterable(FilterOperator.Equals)]
        public bool? IsActive { get; set; }
    }
}
