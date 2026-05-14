using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents room-wise minus data in the PTIS system.
/// Maps to PTIS.RoomWiseMinusData table.
/// </summary>
[Table("RoomWiseMinusData", Schema = "PTIS")]
public class RoomWiseMinusDataEntity : BaseEntity, IHardDeletable
{
    public int RoomWiseSubmissionId { get; set; }

    public decimal? LengthMtr { get; set; }

    public decimal? WidthMtr { get; set; }

    public decimal? AreaSqMtr { get; set; }

    public decimal? HeightMtr { get; set; }

    public decimal? Base1Mtr { get; set; }

    public decimal? Base2Mtr { get; set; }

    public string? Shape { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    /// <summary>
    /// Navigation property to parent RoomWiseSubmissionDetails
    /// </summary>
    public virtual RoomWiseSubmissionDetailsEntity? RoomWiseSubmissionDetails { get; set; }
}
