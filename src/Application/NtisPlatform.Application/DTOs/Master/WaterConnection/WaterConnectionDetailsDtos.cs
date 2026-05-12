using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterConnectionDetailsDto : BaseDtos
{
    public int WaterConnectionId { get; set; }
    public string? ConnectionNo { get; set; }
    public int FinanceYearId { get; set; }
    public string? YearCode { get; set; }
    public DateTime BillDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int ChargeMonths { get; set; }
    public decimal YearlyRate { get; set; }
    public decimal WaterBill { get; set; }
}

public class CreateWaterConnectionDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "WaterConnectionId_Required")]
    public int WaterConnectionId { get; set; }

    [Required(ErrorMessage = "FinanceYearId_Required")]
    public int FinanceYearId { get; set; }

    [Required(ErrorMessage = "BillDate_Required")]
    public DateTime BillDate { get; set; }

    [Required(ErrorMessage = "FromDate_Required")]
    public DateTime FromDate { get; set; }

    [Required(ErrorMessage = "ToDate_Required")]
    public DateTime ToDate { get; set; }

    [Range(1, 12, ErrorMessage = "ChargeMonths_Range")]
    public int ChargeMonths { get; set; }

    [Range(0, 9999999.99, ErrorMessage = "YearlyRate_Range")]
    public decimal YearlyRate { get; set; }

    [Range(0, 9999999.99, ErrorMessage = "WaterBill_Range")]
    public decimal WaterBill { get; set; }
}

public class UpdateWaterConnectionDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "WaterConnectionId_Required")]
    public int WaterConnectionId { get; set; }

    [Required(ErrorMessage = "FinanceYearId_Required")]
    public int FinanceYearId { get; set; }

    [Required(ErrorMessage = "BillDate_Required")]
    public DateTime BillDate { get; set; }

    [Required(ErrorMessage = "FromDate_Required")]
    public DateTime FromDate { get; set; }

    [Required(ErrorMessage = "ToDate_Required")]
    public DateTime ToDate { get; set; }

    [Range(1, 12, ErrorMessage = "ChargeMonths_Range")]
    public int ChargeMonths { get; set; }

    [Range(0, 9999999.99, ErrorMessage = "YearlyRate_Range")]
    public decimal YearlyRate { get; set; }

    [Range(0, 9999999.99, ErrorMessage = "WaterBill_Range")]
    public decimal WaterBill { get; set; }
}
