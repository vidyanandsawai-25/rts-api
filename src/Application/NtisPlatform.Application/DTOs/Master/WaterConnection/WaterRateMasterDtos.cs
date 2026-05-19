using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterRateMasterDto : BaseDtos
{
    public int WaterConnectionTypeId { get; set; }
    public string ConnectionTypeName { get; set; } = string.Empty;
    public int WaterConnectionSizeId { get; set; }
    public string ConnectionSizeDisplay { get; set; } = string.Empty;
    public int FinanceYearId { get; set; }
    public string? YearCode { get; set; }
    public decimal YearlyRate { get; set; }
}

public class CreateWaterRateMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "WaterRateMaster_WaterConnectionTypeId_Required")]
    public int WaterConnectionTypeId { get; set; }

    [Required(ErrorMessage = "WaterRateMaster_WaterConnectionSizeId_Required")]
    public int WaterConnectionSizeId { get; set; }

    [Required(ErrorMessage = "WaterRateMaster_FinanceYearId_Required")]
    public int FinanceYearId { get; set; }

    [Required(ErrorMessage = "WaterRateMaster_YearlyRate_Required")]
    [Range(0, 10000000, ErrorMessage = "YearlyRate_Range")]
    public decimal YearlyRate { get; set; }
}

public class UpdateWaterRateMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "WaterRateMaster_WaterConnectionTypeId_Required")]
    public int WaterConnectionTypeId { get; set; }

    [Required(ErrorMessage = "WaterRateMaster_WaterConnectionSizeId_Required")]
    public int WaterConnectionSizeId { get; set; }

    [Required(ErrorMessage = "WaterRateMaster_FinanceYearId_Required")]
    public int FinanceYearId { get; set; }

    [Required(ErrorMessage = "WaterRateMaster_YearlyRate_Required")]
    [Range(0, 10000000, ErrorMessage = "WaterRateMaster_YearlyRate_Range")]
    public decimal YearlyRate { get; set; }
}
