using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.DataEntrySameAs;

/// <summary>
/// Lookup parameters for finding sibling properties (same Ward + PropertyNo, different partition/wing)
/// that are candidate destinations for a "Same As" copy.
/// </summary>
public class DataEntrySameAsQueryParameters
{
    [Required]
    public int WardId { get; set; }

    [Required]
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional. When supplied, rows with this partition are excluded from the result
    /// (e.g. the source property's own partition). When omitted, all matching properties are returned.
    /// </summary>
    public string? PartitionNo { get; set; }
}
