using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax;

/// <summary>
/// Lookup parameters that identify the single property whose retrospective (year-wise pending)
/// tax details the screen displays. WardId + PropertyNo (+ optional PartitionNo) must resolve to
/// exactly one property.
/// </summary>
public class RetrospectiveTaxQueryParameters
{
    [Required]
    public int WardId { get; set; }

    [Required]
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional. When supplied, only the property with this partition is matched.
    /// When omitted, properties with an empty/null PartitionNo are matched; if more than one
    /// property matches, the request is rejected and the caller must specify a PartitionNo.
    /// </summary>
    public string? PartitionNo { get; set; }
}
