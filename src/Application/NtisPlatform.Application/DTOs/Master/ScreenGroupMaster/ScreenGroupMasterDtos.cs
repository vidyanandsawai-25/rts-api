using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.ScreenGroupMaster;

/// <summary>
/// DTO for ScreenGroupMaster
/// </summary>
public class ScreenGroupMasterDto : BaseDtos
{
    public int ScreenGroupId { get; set; }
    public string? ScreenGroupCode { get; set; }
    public string? ScreenGroupName { get; set; }
    public string? ScreenGroupNameLocal { get; set; }
    public string? ScreenGroupIcon { get; set; }
    public int? DisplayOrder { get; set; } 
}

/// <summary>
/// DTO for creating a new ScreenGroupMaster
/// </summary>
public class CreateScreenGroupMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ScreenGroupCode_Required")]
    [StringLength(50, ErrorMessage = "ScreenGroupCode_MaxLen_50")]
    public string ScreenGroupCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenGroupName_Required")]
    [StringLength(200, ErrorMessage = "ScreenGroupName_MaxLen_200")]
    public string ScreenGroupName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenGroupNameLocal_MaxLen_200")]
    public string? ScreenGroupNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ScreenGroupIcon_MaxLen_100")]
    public string? ScreenGroupIcon { get; set; }

    public int? DisplayOrder { get; set; } 
}

/// <summary>
/// DTO for updating a ScreenGroupMaster
/// </summary>
public class UpdateScreenGroupMasterDto :UpdateBaseDtos
{
    [Required(ErrorMessage = "ScreenGroupCode_Required")]
    [StringLength(50, ErrorMessage = "ScreenGroupCode_MaxLen_50")]
    public string ScreenGroupCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenGroupName_Required")]
    [StringLength(200, ErrorMessage = "ScreenGroupName_MaxLen_200")]
    public string ScreenGroupName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenGroupNameLocal_MaxLen_200")]
    public string? ScreenGroupNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ScreenGroupIcon_MaxLen_100")]
    public string? ScreenGroupIcon { get; set; }

    public int? DisplayOrder { get; set; } 
}
