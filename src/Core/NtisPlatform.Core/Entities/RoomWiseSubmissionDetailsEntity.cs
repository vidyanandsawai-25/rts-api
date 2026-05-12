using NtisPlatform.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("RoomWiseSubmissionDetails", Schema = "PTIS")]
public class RoomWiseSubmissionDetailsEntity : BaseEntity
{
    public int? PropertyId { get; set; }

    [Required]
    public int PropertyDetailsId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LengthMtr { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WidthMtr { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AreaSqMtr { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? HeightMtr { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Base1Mtr { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Base2Mtr { get; set; }

    public int? NoOfRooms { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAreaSqMtr { get; set; }

    [StringLength(25)]
    public string? Shape { get; set; }

    [StringLength(100)]
    public string? RoomNo { get; set; }

    public bool? OuterYesNo { get; set; }

    [StringLength(100)]
    public string? RoomType { get; set; }

    [StringLength(100)]
    public string? SubmissionType { get; set; }

    public bool? MinusYesNo { get; set; } = false;

    public bool? MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}