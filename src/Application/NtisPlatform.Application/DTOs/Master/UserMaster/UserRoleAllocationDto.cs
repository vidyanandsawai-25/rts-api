using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.UserMaster
{
    public class UserRoleAllocationDto: BaseDtos
    {
        [Required(ErrorMessage = "UserId_Required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "UserRoleId_Required")]
        public int UserRoleId { get; set; }
        public string? DepartmentName { get; set; }
        public string? UserRoleName { get; set; }
    }
    public class UserRoleAllocationCreateDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "UserRoleId_Required")]
        public int UserRoleId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UserRoleAllocationUpdateDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "UserRoleId_Required")]
        public int UserRoleId { get; set; }
        public DateTime UpdatedDate { get; set; } 
    }
}
