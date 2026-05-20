namespace NtisPlatform.Core.Entities.Master;

public class WaterConnectionStatusEntity : BaseEntity
{
    public string StatusName { get; set; } = string.Empty;
    public ICollection<WaterConnectionMasterEntity> WaterConnectionMaster { get; set; } = new List<WaterConnectionMasterEntity>();

}
