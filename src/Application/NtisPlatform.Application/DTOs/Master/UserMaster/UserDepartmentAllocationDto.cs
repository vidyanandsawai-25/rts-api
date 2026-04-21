using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.UserMaster;

public class UserDepartmentAllocationDto : BaseDtos
{
    [Required(ErrorMessage = "UserId_Required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "DepartmentId_Required")]
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentNameLocal { get; set; }
}

public class UserDepartmentAllocationCreateDto : CreateBaseDtos
{
    [Required(ErrorMessage = "DepartmentId_Required")]
    public int DepartmentId { get; set; }
}

public class UserDepartmentAllocationUpdateDto:UpdateBaseDtos
{
    [Required(ErrorMessage = "DepartmentId_Required")]
    public int DepartmentId { get; set; }
}



