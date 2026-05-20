namespace NtisPlatform.Core.Entities.Master;

public class WaterConnectionSizeEntity : BaseEntity
{
    public decimal ConnectionSize { get; set; }
    public string ConnectionSizeUnit { get; set; } = string.Empty;
    public ICollection<WaterConnectionMasterEntity> WaterConnectionMaster { get; set; } = new List<WaterConnectionMasterEntity>();
    public ICollection<WaterRateMasterEntity> WaterRateMaster { get; set; } = new List<WaterRateMasterEntity>();
}
