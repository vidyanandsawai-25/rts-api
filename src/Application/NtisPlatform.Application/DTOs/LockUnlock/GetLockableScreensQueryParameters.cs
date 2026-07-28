namespace NtisPlatform.Application.DTOs.LockUnlock;

/// <summary>
/// Query parameters for filtering and searching lockable screens.
/// </summary>
public class GetLockableScreensQueryParameters
{
    /// <summary>
    /// Optional search term to search across ScreenName, ScreenNameLocal, ModuleName, and ModuleNameLocal.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Optional filter by specific screen ID.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Optional filter by module IDs as comma-separated values (e.g., "1,2,3").
    /// </summary>
    public string? ModuleIds { get; set; }
}
