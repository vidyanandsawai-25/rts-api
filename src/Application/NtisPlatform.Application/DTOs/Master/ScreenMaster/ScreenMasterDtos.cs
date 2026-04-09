using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// DTO for ScreenMaster
/// </summary>
public class ScreenMasterDto : BaseDtos
{
    public int Id { get; set; }
    public int ScreenGroupId { get; set; }
    public int? ModuleId { get; set; }
    public string? ScreenCode { get; set; }
    public string? ScreenName { get; set; }
    public string? ScreenNameLocal { get; set; }
    public string? ScreenIcon { get; set; }
    public string? RoutePath { get; set; }
    public bool? IsMenu { get; set; }
    public bool? IsAuthenticationRequired { get; set; }
    
    public int? DisplayOrder { get; set; }
 
}

/// <summary>
/// DTO for creating a new ScreenMaster
/// </summary>
public class CreateScreenMasterDto: CreateBaseDtos
{
    [Required(ErrorMessage = "ScreenGroupId_Required")]
    public int ScreenGroupId { get; set; }

    public int? ModuleId { get; set; }

    [Required(ErrorMessage = "ScreenCode_Required")]
    [StringLength(50, ErrorMessage = "ScreenCode_MaxLen_50")]
    public string ScreenCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenName_Required")]
    [StringLength(200, ErrorMessage = "ScreenName_MaxLen_200")]
    public string ScreenName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenNameLocal_MaxLen_200")]
    public string? ScreenNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ScreenIcon_MaxLen_100")]
    public string? ScreenIcon { get; set; }

    [StringLength(500, ErrorMessage = "RoutePath_MaxLen_500")]
    public string? RoutePath { get; set; }

    public bool IsMenu { get; set; } = true;

    public bool IsAuthenticationRequired { get; set; } = true; 

    public int? DisplayOrder { get; set; }
}

/// <summary>
/// DTO for updating a ScreenMaster
/// </summary>
public class UpdateScreenMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ScreenGroupId_Required")]
    public int ScreenGroupId { get; set; }

    public int? ModuleId { get; set; }

    [Required(ErrorMessage = "ScreenCode_Required")]
    [StringLength(50, ErrorMessage = "ScreenCode_MaxLen_50")]
    public string ScreenCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenName_Required")]
    [StringLength(200, ErrorMessage = "ScreenName_MaxLen_200")]
    public string ScreenName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenNameLocal_MaxLen_200")]
    public string? ScreenNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ScreenIcon_MaxLen_100")]
    public string? ScreenIcon { get; set; }

    [StringLength(500, ErrorMessage = "RoutePath_MaxLen_500")]
    public string? RoutePath { get; set; }
    public bool IsMenu { get; set; }
    public bool IsAuthenticationRequired { get; set; }
    public int? DisplayOrder { get; set; }
}
