using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Property;

/// <summary>
/// Query parameters for the Property-Wise Search screen: find the exact property (or properties)
/// under a ward, identified by PropertyNo and, when needed to disambiguate multiple units under
/// the same PropertyNo, PartitionNo. Thin wrapper over the existing BuildingWise
/// <see cref="PropertySearchCategory"/> scope.
/// </summary>
public class PropertyWiseSearchQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Ward to search within. Required.
    /// </summary>
    public int WardId { get; set; }

    /// <summary>
    /// Property number to match exactly within the ward. Required.
    /// </summary>
    public string? PropertyNo { get; set; }

    /// <summary>
    /// Partition/unit number to match exactly. Optional - when omitted, all partitions under the
    /// given WardId/PropertyNo are returned.
    /// </summary>
    public string? PartitionNo { get; set; }
}
