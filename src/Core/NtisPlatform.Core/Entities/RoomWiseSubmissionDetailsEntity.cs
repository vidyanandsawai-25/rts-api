using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;
public class RoomWiseSubmissionDetailsEntity : BaseEntity, IHardDeletable
{
    public int? PropertyDetailsId { get; set; }      // FK to PropertyDetails — required
    public int? PropertyId { get; set; }             // FK to PropertyMast — optional reference
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? HeightMtr { get; set; }
    public double? Base1Mtr { get; set; }
    public double? Base2Mtr { get; set; }
    public int? NoOfRooms { get; set; }
    public double? TotalAreaSqMtr { get; set; }
    public string? Shape { get; set; }
    public string? RoomNo { get; set; }
    public bool OuterYesNo { get; set; } = false;
    public string? RoomType { get; set; }
    public string? SubmissionType { get; set; }
    public bool MinusYesNo { get; set; } = false;
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
    public virtual PropertyEntity? PropertyMast { get; set; }
    public virtual ICollection<RoomWiseMinusDataEntity> PropertyRoomMinus { get; set; } = new List<RoomWiseMinusDataEntity>();
}
