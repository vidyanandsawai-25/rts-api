using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.UserRoleMaster
{
    public class UserRoleMasterDto : BaseDtos
    {
        public int Id { get; set; } = 0;
        public string UserRoleName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class CreateUserRoleMasterDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "UserRoleName_Required")]
        [StringLength(100, ErrorMessage = "UserRoleName_MaxLen_100")]
        public string UserRoleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }
    }

    public class UpdateUserRoleMasterDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "UserRoleName_Required")]
        [StringLength(100, ErrorMessage = "UserRoleName_MaxLen_100")]
        public string UserRoleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }
    }
    
    public class UserRoleMasterQueryParameterDto : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        public string? UserRoleName { get; set; }
        [Filterable(FilterOperator.Equals)]
        public bool? IsActive { get; set; }
        [Filterable(FilterOperator.Equals)]
        public int? DepartmentId { get; set; }
    }
}
