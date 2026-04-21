using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.UserMaster
{
    public class UserModuleAllocationDto: BaseDtos
    {
        [Required(ErrorMessage = "UserId_Required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "ModuleId_Required")]
        public int ModuleId { get; set; }
        public string? DepartmentName { get; set; }
        public string? ModuleName { get; set; }
        public string? ModuleNameLocal { get; set; } = null;
    }
    public class UserModuleAllocationCreateDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "ModuleId_Required")]
        public int ModuleId { get; set; }
    }

    public class UserModuleAllocationUpdateDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "DepartmentId_Required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "ModuleId_Required")]
        public int ModuleId { get; set; }
    }

}
