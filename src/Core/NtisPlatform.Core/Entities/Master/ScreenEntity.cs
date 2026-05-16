namespace NtisPlatform.Core.Entities.Master;

public class ScreenEntity : BaseEntity
{
    public string ScreenName { get; set; } = string.Empty;
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