namespace NtisPlatform.Core.Entities.Master;

public class BlockMasterEntity : BaseEntity
{
    public int WardId { get; set; }
    public string BlockNo { get; set; } = string.Empty;
    public virtual WardEntity? Ward { get; set; }
}
