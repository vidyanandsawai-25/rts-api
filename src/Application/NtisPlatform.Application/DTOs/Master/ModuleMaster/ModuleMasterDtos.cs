using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.ModuleMaster;

/// <summary>
/// DTO for ModuleMaster
/// </summary>
public class ModuleMasterDto :CommonBaseDtos
{
    public int ModuleId { get; set; }
    public int DepartmentId { get; set; }
    public string? ModuleCode { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleNameLocal { get; set; }
    public string? ModuleIcon { get; set; }
    public string? ModuleLabel { get; set; }
    public string? ModuleDescription { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// DTO for creating a new ModuleMaster
/// </summary>
public class CreateModuleMasterDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "DepartmentId_Required")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "ModuleCode_Required")]
    [StringLength(50, ErrorMessage = "ModuleCode_MaxLen_50")]
    public string ModuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ModuleName_Required")]
    [StringLength(200, ErrorMessage = "ModuleName_MaxLen_200")]
    public string ModuleName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ModuleNameLocal_MaxLen_200")]
    public string? ModuleNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ModuleIcon_MaxLen_100")]
    public string? ModuleIcon { get; set; }

    [StringLength(100, ErrorMessage = "ModuleLabel_MaxLen_100")]
    public string? ModuleLabel { get; set; }

    [StringLength(500, ErrorMessage = "ModuleDescription_MaxLen_500")]
    public string? ModuleDescription { get; set; }
}

/// <summary>
/// DTO for updating a ModuleMaster
/// </summary>
public class UpdateModuleMasterDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "DepartmentId_Required")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "ModuleId_Required")]
    public int ModuleId { get; set; }

    [Required(ErrorMessage = "ModuleCode_Required")]
    [StringLength(50, ErrorMessage = "ModuleCode_MaxLen_50")]
    public string ModuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ModuleName_Required")]
    [StringLength(200, ErrorMessage = "ModuleName_MaxLen_200")]
    public string ModuleName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ModuleNameLocal_MaxLen_200")]
    public string? ModuleNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ModuleIcon_MaxLen_100")]
    public string? ModuleIcon { get; set; }

    [StringLength(100, ErrorMessage = "ModuleLabel_MaxLen_100")]
    public string? ModuleLabel { get; set; }

    [StringLength(500, ErrorMessage = "ModuleDescription_MaxLen_500")]
    public string? ModuleDescription { get; set; }
 
}
