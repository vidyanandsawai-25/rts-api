namespace NtisPlatform.Core.Entities.Master;

public class WaterConnectionMasterEntity : BaseEntity
{
    public int PropertyId { get; set; }
    public int WaterConnectionTypeId { get; set; }
    public int WaterConnectionSizeId { get; set; }
    public int? WaterConnectionStatusId { get; set; }
    public string ConnectionNo { get; set; } = string.Empty;
    public string? MeterNo { get; set; }
    public DateTime ConnectionStartDate { get; set; }
    public DateTime? ConnectionStopDate { get; set; }

    public WaterConnectionTypeEntity WaterConnectionType { get; set; } = null!;
    public WaterConnectionSizeEntity WaterConnectionSize { get; set; } = null!;
    public WaterConnectionStatusEntity? WaterConnectionStatus { get; set; }
    public ICollection<WaterConnectionDetailsEntity> Details { get; set; } = [];
}
