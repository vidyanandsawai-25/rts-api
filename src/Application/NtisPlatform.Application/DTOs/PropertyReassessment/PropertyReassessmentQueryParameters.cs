using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyReassessment;

/// <summary>
/// Lookup parameters that identify the single property whose re-assessment (old vs new)
/// data the screen displays. Mirrors STEP 1 of the legacy re-assessment SQL:
/// WardId + PropertyNo (+ optional PartitionNo) must resolve to exactly one property.
/// </summary>
public class PropertyReassessmentQueryParameters
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
