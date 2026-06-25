namespace NtisPlatform.Application.DTOs.DataEntrySameAs;

/// <summary>
/// Summary of a "Data Entry Same As" operation.
/// </summary>
public class DataEntrySameAsResultDto
{
    public int SourcePropertyId { get; set; }

    /// <summary>Number of valid destination properties processed.</summary>
    public int ProcessedDestinations { get; set; }

    /// <summary>Number of supplied destination ids dropped (self-reference / non-existent).</summary>
    public int SkippedDestinations { get; set; }

    /// <summary>New PropertyDetails rows created across all destinations.</summary>
    public int PropertyDetailsCopied { get; set; }

    /// <summary>New RoomWiseSubmissionDetails rows created across all destinations.</summary>
    public int RoomSubmissionsCopied { get; set; }

    /// <summary>New RoomWiseMinusData rows created across all destinations.</summary>
    public int RoomMinusCopied { get; set; }

    /// <summary>Number of PropertyMast rows whose Type was updated (TYPEWISE only).</summary>
    public int TypeUpdatedProperties { get; set; }

    /// <summary>Number of BuildingPlanType rows inserted (TYPEWISE only; unique on PropertyId+Type, insert-only).</summary>
    public int BuildingPlanTypeInserted { get; set; }

    /// <summary>Non-fatal notes (e.g. dropped destination ids).</summary>
    public List<string> Warnings { get; set; } = [];
}
