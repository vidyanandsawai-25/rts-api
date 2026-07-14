using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents society wing details in the GSMS system.
/// Maps to [GSMS].[SocietyWingDetails] table.
/// </summary>
[Table("SocietyWingDetails", Schema = "GSMS")]
public class SocietyWingDetailsEntity : BaseEntity
{
    public int? WingId { get; set; }

    public int? PropertyId { get; set; }

    public int? SocietyDetailId { get; set; }

    public string? FromFloor { get; set; }

    public string? ToFloor { get; set; }

    public string? OldWingName { get; set; }

    public string? NewWingName { get; set; }

    public int? NoOfFlat { get; set; }

    public int? NoOfShop { get; set; }

    public int? NoOfRowHouse { get; set; }

    public int? WingPhoto { get; set; }

    public int? BoardPhoto { get; set; }

    // Navigation properties
    [ForeignKey(nameof(WingId))]
    public virtual WingEntity? WingMaster { get; set; }

    [ForeignKey(nameof(SocietyDetailId))]
    public virtual SocietyDetailsEntity? SocietyDetailsMast { get; set; }
}
