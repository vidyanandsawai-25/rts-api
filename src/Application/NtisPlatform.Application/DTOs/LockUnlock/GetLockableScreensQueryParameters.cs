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
    /// Optional filter by specific module ID.
    /// </summary>
    public int? ModuleId { get; set; }
}
