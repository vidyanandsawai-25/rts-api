namespace NtisPlatform.Core.Entities.Master;

public class WaterRateMasterEntity : BaseEntity
{
    public int WaterConnectionTypeId { get; set; }
    public int WaterConnectionSizeId { get; set; }
    public int FinanceYearId { get; set; }
    public decimal YearlyRate { get; set; }

    public WaterConnectionTypeEntity WaterConnectionType { get; set; } = null!;
    public WaterConnectionSizeEntity WaterConnectionSize { get; set; } = null!;
    public YearMasterEntity FinanceYear { get; set; } = null!;
}
