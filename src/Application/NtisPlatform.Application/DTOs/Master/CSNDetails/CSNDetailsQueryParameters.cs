using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.CSNDetails;

/// <summary>
/// Query parameters for filtering and searching CSN Details
/// </summary>
public class CSNDetailsQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Filter by Rate Master CV
    /// </summary>
    [Filterable]
    public int? RateMasterCVId { get; set; }

    /// <summary>
    /// Filter by CSN (City Survey Number)
    /// </summary>
    [Filterable]
    public string? CSN { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Filterable]
    public bool? IsActive { get; set; }
}
