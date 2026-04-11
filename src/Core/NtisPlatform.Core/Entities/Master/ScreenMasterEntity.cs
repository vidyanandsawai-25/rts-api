namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Entity representing a screen in the system
/// </summary>
public class ScreenMasterEntity : BaseEntity
{
    /// <summary>
    /// Unique identifier for the screen
    /// </summary>    /// <summary>
    /// Foreign key to screen group
    /// </summary>
    public int ScreenGroupId { get; set; }

    /// <summary>
    /// Foreign key to module
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>
    /// Unique code for the screen
    /// </summary>
    public string? ScreenCode { get; set; }

    /// <summary>
    /// Name of the screen
    /// </summary>
    public string? ScreenName { get; set; }

    /// <summary>
    /// Name of the screen in local language
    /// </summary>
    public string? ScreenNameLocal { get; set; }

    /// <summary>
    /// Icon for the screen
    /// </summary>
    public string? ScreenIcon { get; set; }

    /// <summary>
    /// Route path for navigation
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// Whether the screen is a menu item
    /// </summary>
    public bool? IsMenu { get; set; }

    /// <summary>
    /// Whether authentication is required for this screen
    /// </summary>
    public bool? IsAuthenticationRequired { get; set; }
 

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Navigation property to the screen group
    /// </summary>
    public ScreenGroupMasterEntity? ScreenGroup { get; set; }

    /// <summary>
    /// Navigation property to the module
    /// </summary>
    public ModuleMasterEntity? Module { get; set; }
}
