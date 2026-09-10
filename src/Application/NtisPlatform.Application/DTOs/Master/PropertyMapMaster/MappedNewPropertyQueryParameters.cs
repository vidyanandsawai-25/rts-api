using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

/// <summary>
/// Query parameters for retrieving new properties mapped to an Old Property ID.
/// </summary>
public class MappedNewPropertyQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Old Property ID (PropertyMastOld.Id)
    /// </summary>
    public int? OldPropertyId { get; set; }

    /// <summary>
    /// Backward-compatible alias for OldPropertyId (PropertyMastOld.Id).
    /// </summary>
    [Obsolete("Use OldPropertyId instead.")]
    public int? PropertyId { get; set; }
}
