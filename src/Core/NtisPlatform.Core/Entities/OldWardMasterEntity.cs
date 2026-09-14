namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents the Old Ward Master entity.
/// </summary>
public class OldWardMasterEntity : BaseEntity
{
    public string OldWardNo { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int OldZoneId { get; set; }

    public int? SequenceNo { get; set; }
}