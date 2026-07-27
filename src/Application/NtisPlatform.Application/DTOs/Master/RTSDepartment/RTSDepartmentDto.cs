using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RTSDepartmentMaster;

public class RTSDepartmentDto : BaseDtos
{
    public string DepartmentName { get; set; } = string.Empty;
    public string? DepartmentNameLocal { get; set; }
    public string? DepartmentIcon { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateRTSDepartmentDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RTS_DepartmentName_Required")]
    [StringLength(100, ErrorMessage = "DepartmentName_MaxLen_100")]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DepartmentNameLocal_MaxLen_200")]
    public string? DepartmentNameLocal { get; set; }

    [StringLength(200, ErrorMessage = "DepartmentIcon_MaxLen_200")]
    public string? DepartmentIcon { get; set; }

    public int DisplayOrder { get; set; }
}

public class UpdateRTSDepartmentDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RTS_DepartmentName_Required")]
    [StringLength(100, ErrorMessage = "DepartmentName_MaxLen_100")]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DepartmentNameLocal_MaxLen_200")]
    public string? DepartmentNameLocal { get; set; }

    [StringLength(200, ErrorMessage = "DepartmentIcon_MaxLen_200")]
    public string? DepartmentIcon { get; set; }

    public int DisplayOrder { get; set; }
}
