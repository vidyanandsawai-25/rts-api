using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

/// <summary>
/// Query parameters for retrieving old properties mapped/merged to a New Property ID.
/// </summary>
public class MappedOldPropertyQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// New Property ID (PropertyMast.Id)
    /// </summary>
    public int? PropertyId { get; set; }

    /// <summary>
    /// Backward-compatible alias for PropertyId (New Property ID).
    /// </summary>
    [Obsolete("Use PropertyId instead.")]
    public int? NewPropertyId { get; set; }
}
