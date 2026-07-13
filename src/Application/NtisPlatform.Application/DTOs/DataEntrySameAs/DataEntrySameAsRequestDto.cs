using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.DataEntrySameAs;

/// <summary>
/// Request to make one or more destination properties' data-entry the SAME AS a source property.
/// Re-implements the legacy [PTIS].[DataEntrySameAS] stored procedure in clean-architecture code:
/// copies PropertyDetails -> RoomWiseSubmissionDetails -> RoomWiseMinusData from the source to each
/// destination, after soft-deleting the destination's matching data-entry (replace semantics).
/// </summary>
public class DataEntrySameAsRequestDto
{
    /// <summary>
    /// The property whose data-entry is copied to the destinations.
    /// </summary>
    [Required]
    public int SourcePropertyId { get; set; }

    /// <summary>
    /// The properties that receive a copy of the source's data-entry.
    /// Self-references and non-existent ids are dropped (reported as warnings).
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "DataEntrySameAs_DestinationPropertyIds_Required")]
    public List<int> DestinationPropertyIds { get; set; } = [];

    /// <summary>
    /// Filter mode driving which PropertyDetails rows are acted on:
    /// PARKING (parking TypeOfUse rows only), TYPEWISE (Type stamp only) or PROPERTYWISE (non-parking rows only).
    /// Accepts a single value or a comma-separated list (e.g. "PARKING,PROPERTYWISE"); each listed mode
    /// is applied within the same transaction.
    /// </summary>
    [Required]
    public string FilterType { get; set; } = string.Empty;

    /// <summary>
    /// TYPEWISE only. 1-99 = manual Type stamped on source and qualifying destinations;
    /// 0 (default) = copy the source's own Type to the destinations.
    /// </summary>
    public int Type { get; set; } = 0;
}
