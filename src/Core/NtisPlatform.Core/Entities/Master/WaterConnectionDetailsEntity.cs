namespace NtisPlatform.Core.Entities.Master;

public class WaterConnectionDetailsEntity : BaseEntity
{
    public int WaterConnectionId { get; set; }
    public int FinanceYearId { get; set; }
    public DateTime BillDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int ChargeMonths { get; set; }
    public decimal YearlyRate { get; set; }
    public decimal WaterBill { get; set; }

    public WaterConnectionMasterEntity WaterConnection { get; set; } = null!;
    public YearMasterEntity FinanceYear { get; set; } = null!;
}
