namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a Wing master entity.
/// </summary>
public class WingEntity : BaseEntity
{
    public int WingId { get; set; }
    public string WingNo { get; set; } = string.Empty;
    public int? SequenceNo { get; set; }
}