using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.DesignationMaster;

/// <summary>
/// DTO for DesignationMaster
/// </summary>
public class DesignationMasterDto : CommonBaseDtos
{
    public int DesignationMasterId { get; set; }
    public string? DesignationCode { get; set; }
    public string? DesignationName { get; set; }
    public string? DesignationLocal { get; set; }
    public string? DesignationDescription { get; set; }    
}

/// <summary>
/// DTO for creating a new DesignationMaster
/// </summary>
public class CreateDesignationMasterDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "Designationcode_Required")]
    [StringLength(50, ErrorMessage = "Designationcode_MaxLen_50")]
    public string DesignationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DesignationName_Required")]
    [StringLength(200, ErrorMessage = "DesignationName_MaxLen_200")]
    public string DesignationName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DesignationLocal_MaxLen_200")]
    public string? DesignationLocal { get; set; }

    [StringLength(500, ErrorMessage = "DesignationDescription_MaxLen_500")]
    public string? DesignationDescription { get; set; }

}

/// <summary>
/// DTO for updating a DesignationMaster
/// </summary>
public class UpdateDesignationMasterDto: UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "Designationcode_Required")]
    [StringLength(50, ErrorMessage = "Designationcode_MaxLen_50")]
    public string DesignationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DesignationName_Required")]
    [StringLength(200, ErrorMessage = "DesignationName_MaxLen_200")]
    public string DesignationName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DesignationLocal_MaxLen_200")]
    public string? DesignationLocal { get; set; }

    [StringLength(500, ErrorMessage = "DesignationDescription_MaxLen_500")]
    public string? DesignationDescription { get; set; }
}
