using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.DepartmentMaster;

/// <summary>
/// DTO for DepartmentMaster
/// </summary>
public class DepartmentMasterDto :BaseDtos
{
    public int Id { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentNameLocal { get; set; }
    public string? DepartmentIcon { get; set; }
    public string? DepartmentDescription { get; set; }
 
}

/// <summary>
/// DTO for creating a new DepartmentMaster
/// </summary>
public class CreateDepartmentMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "DepartmentCode_Required")]
    [StringLength(50, ErrorMessage = "DepartmentCode_MaxLen_50")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DepartmentName_Required")]
    [StringLength(200, ErrorMessage = "DepartmentName_MaxLen_200")]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DepartmentNameLocal_MaxLen_200")]
    public string? DepartmentNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "DepartmentIcon_MaxLen_100")]
    public string? DepartmentIcon { get; set; }

    [StringLength(500, ErrorMessage = "DepartmentDescription_MaxLen_500")]
    public string? DepartmentDescription { get; set; }

}

/// <summary>
/// DTO for updating a DepartmentMaster
/// </summary>
public class UpdateDepartmentMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "DepartmentCode_Required")]
    [StringLength(50, ErrorMessage = "DepartmentCode_MaxLen_50")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DepartmentName_Required")]
    [StringLength(200, ErrorMessage = "DepartmentName_MaxLen_200")]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DepartmentNameLocal_MaxLen_200")]
    public string? DepartmentNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "DepartmentIcon_MaxLen_100")]
    public string? DepartmentIcon { get; set; }

    [StringLength(500, ErrorMessage = "DepartmentDescription_MaxLen_500")]
    public string? DepartmentDescription { get; set; }

   
}
