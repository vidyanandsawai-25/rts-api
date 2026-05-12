namespace NtisPlatform.Core.Entities.Master;

public class WaterConnectionSizeEntity : BaseEntity
{
    public decimal ConnectionSize { get; set; }
    public string ConnectionSizeUnit { get; set; } = string.Empty;
}
