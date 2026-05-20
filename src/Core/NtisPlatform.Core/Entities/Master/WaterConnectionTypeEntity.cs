namespace NtisPlatform.Core.Entities.Master;

public class WaterConnectionTypeEntity : BaseEntity
{
    public string ConnectionTypeCode { get; set; } = string.Empty;
    public string ConnectionTypeName { get; set; } = string.Empty;
    public ICollection<WaterConnectionMasterEntity> WaterConnectionMaster { get; set; } = new List<WaterConnectionMasterEntity>();
    public ICollection<WaterRateMasterEntity> WaterRateMaster { get; set; } = new List<WaterRateMasterEntity>();
}
