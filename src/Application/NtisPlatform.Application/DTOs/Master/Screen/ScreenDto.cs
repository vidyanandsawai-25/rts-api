using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class ScreenDto : BaseDtos
{
    public string ScreenName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? ModuleId { get; set; }
    public string ScreenCode { get; set; } = string.Empty;
    public string? ScreenNameLocal { get; set; }
    public string? ScreenIcon { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsAuthenticationRequired { get; set; }
    public int? ParentScreenId { get; set; }
    public int? MenuLevel { get; set; }
    public string? RoutePath { get; set; }
    public string? BaseRoutePath { get; set; }
    public string? RouteParamPattern { get; set; }
    public string? Purpose { get; set; }
    public string? ComponentName { get; set; }
    public string? AreaName { get; set; }
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public bool IsMenuVisible { get; set; }
}

public class CreateScreenDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ScreenMaster_ScreenName_Required")]
    [StringLength(200, ErrorMessage = "ScreenMaster_ScreenName_MaxLen_200")]
    public string ScreenName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? ModuleId { get; set; }

    [Required(ErrorMessage = "ScreenMaster_ScreenCode_Required")]
    [StringLength(200, ErrorMessage = "ScreenMaster_ScreenCode_MaxLen_200")]
    public string ScreenCode { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenMaster_ScreenNameLocal_MaxLen_200")]
    public string? ScreenNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ScreenMaster_ScreenIcon_MaxLen_100")]
    public string? ScreenIcon { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsAuthenticationRequired { get; set; } = true;
    public int? ParentScreenId { get; set; }
    public int? MenuLevel { get; set; }

    [StringLength(500, ErrorMessage = "ScreenMaster_RoutePath_MaxLen_500")]
    public string? RoutePath { get; set; }

    [StringLength(500, ErrorMessage = "ScreenMaster_BaseRoutePath_MaxLen_500")]
    public string? BaseRoutePath { get; set; }

    [StringLength(500, ErrorMessage = "ScreenMaster_RouteParamPattern_MaxLen_500")]
    public string? RouteParamPattern { get; set; }

    [StringLength(100, ErrorMessage = "ScreenMaster_Purpose_MaxLen_100")]
    public string? Purpose { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_ComponentName_MaxLen_200")]
    public string? ComponentName { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_AreaName_MaxLen_200")]
    public string? AreaName { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_ControllerName_MaxLen_200")]
    public string? ControllerName { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_ActionName_MaxLen_200")]
    public string? ActionName { get; set; }
    public bool? IsMenuVisible { get; set; } = true;
}

public class UpdateScreenDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ScreenMaster_ScreenName_Required")]
    [StringLength(200, ErrorMessage = "ScreenMaster_ScreenName_MaxLen_200")]
    public string ScreenName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? ModuleId { get; set; }

    [Required(ErrorMessage = "ScreenMaster_ScreenCode_Required")]
    [StringLength(200, ErrorMessage = "ScreenMaster_ScreenCode_MaxLen_200")]
    public string ScreenCode { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenMaster_ScreenNameLocal_MaxLen_200")]
    public string? ScreenNameLocal { get; set; }

    [StringLength(100, ErrorMessage = "ScreenMaster_ScreenIcon_MaxLen_100")]
    public string? ScreenIcon { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsAuthenticationRequired { get; set; }
    public int? ParentScreenId { get; set; }
    public int? MenuLevel { get; set; }

    [StringLength(500, ErrorMessage = "ScreenMaster_RoutePath_MaxLen_500")]
    public string? RoutePath { get; set; }

    [StringLength(500, ErrorMessage = "ScreenMaster_BaseRoutePath_MaxLen_500")]
    public string? BaseRoutePath { get; set; }

    [StringLength(500, ErrorMessage = "ScreenMaster_RouteParamPattern_MaxLen_500")]
    public string? RouteParamPattern { get; set; }

    [StringLength(100, ErrorMessage = "ScreenMaster_Purpose_MaxLen_100")]
    public string? Purpose { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_ComponentName_MaxLen_200")]
    public string? ComponentName { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_AreaName_MaxLen_200")]
    public string? AreaName { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_ControllerName_MaxLen_200")]
    public string? ControllerName { get; set; }

    [StringLength(200, ErrorMessage = "ScreenMaster_ActionName_MaxLen_200")]
    public string? ActionName { get; set; }
    public bool? IsMenuVisible { get; set; }
}