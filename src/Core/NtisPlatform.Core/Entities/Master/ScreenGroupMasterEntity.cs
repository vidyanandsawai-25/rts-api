namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a screen group in the system
/// </summary>
public class ScreenGroupMasterEntity : CommonBaseEntity
{
    /// <summary>
    /// Unique identifier for the screen group
    /// </summary>
    public int ScreenGroupId { get; set; } 

    /// <summary>
    /// Unique code for the screen group
    /// </summary>
    public string? ScreenGroupCode { get; set; }

    /// <summary>
    /// Name of the screen group
    /// </summary>
    public string? ScreenGroupName { get; set; }

    /// <summary>
    /// Name of the screen group in local language
    /// </summary>
    public string? ScreenGroupNameLocal { get; set; }

    /// <summary>
    /// Icon for the screen group
    /// </summary>
    public string? ScreenGroupIcon { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int? DisplayOrder { get; set; }
 
}
