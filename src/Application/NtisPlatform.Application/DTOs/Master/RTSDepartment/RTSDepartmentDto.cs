using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RTSDepartmentMaster;

public class RTSDepartmentDto : BaseDtos
{       
    public string DepartmentName { get; set; } = string.Empty;
    public string DeptIcon { get; set; } = string.Empty;

}

public class CreateRTSDepartmentDto : CreateBaseDtos
{

    [Required(ErrorMessage = "RTS_DepartmentName_Required")]
    [StringLength(100, ErrorMessage = "DepartmentName_MaxLen_100")]
    public string DepartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RTS_DeptIcon_Required")]
    public string DeptIcon { get; set; } = string.Empty;
}

public class UpdateRTSDepartmentDto : UpdateBaseDtos
{

    [Required(ErrorMessage = "RTS_DepartmentName_Required")]
    [StringLength(100, ErrorMessage = "DepartmentName_MaxLen_100")]
    public string DepartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RTS_DeptIcon_Required")]
    public string DeptIcon { get; set; } = string.Empty;
}
