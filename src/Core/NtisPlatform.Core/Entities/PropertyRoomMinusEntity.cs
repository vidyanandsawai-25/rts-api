using NtisPlatform.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

[Table("RoomWiseMinusData", Schema = "PTIS")]
public class PropertyRoomMinusEntity : BaseEntity, IHardDeletable
{
    public int RoomWiseSubmissionId { get; set; }   // FK to RoomWiseSubmissionDetails
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? HeightMtr { get; set; }
    public string? Shape { get; set; }
    public double? Base1Mtr { get; set; }
    public double? Base2Mtr { get; set; }

    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual RoomWiseSubmissionDetailsEntity? RoomWiseSubmissionDetails { get; set; }
}
