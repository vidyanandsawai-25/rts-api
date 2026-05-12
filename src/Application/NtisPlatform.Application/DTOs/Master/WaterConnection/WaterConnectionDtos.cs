using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterConnectionDto : BaseDtos
{
    public int PropertyId { get; set; }
    public int WaterConnectionTypeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int WaterConnectionSizeId { get; set; }
    public string TapSize { get; set; } = string.Empty;
    public int? WaterConnectionStatusId { get; set; }
    public string? StatusName { get; set; }
    public string ConnectionNo { get; set; } = string.Empty;
    public string? MeterNo { get; set; }
    public DateTime ConnectionStartDate { get; set; }
    public DateTime? ConnectionStopDate { get; set; }
    public string? InstallDate { get; set; }
    public string? ActivatedDate { get; set; }
    public string? StoppedDate { get; set; }
    public decimal? ApplicableRate { get; set; }
    public decimal? ApplicableCharges { get; set; }
    public string Category { get; set; } = "Yearly";
}

public class CreateWaterConnectionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyId_Required")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "WaterConnectionTypeId_Required")]
    public int WaterConnectionTypeId { get; set; }

    [Required(ErrorMessage = "WaterConnectionSizeId_Required")]
    public int WaterConnectionSizeId { get; set; }

    public int? WaterConnectionStatusId { get; set; }

    private string _connectionNo = string.Empty;

    [Required(ErrorMessage = "ConnectionNo_Required")]
    [StringLength(50, ErrorMessage = "ConnectionNo_MaxLen_50")]
    public string ConnectionNo
    {
        get => _connectionNo;
        set => _connectionNo = value?.Trim() ?? string.Empty;
    }

    public string? MeterNo { get; set; }

    [Required(ErrorMessage = "ConnectionStartDate_Required")]
    public DateTime ConnectionStartDate { get; set; }

    public DateTime? ConnectionStopDate { get; set; }
}

public class UpdateWaterConnectionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyId_Required")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "WaterConnectionTypeId_Required")]
    public int WaterConnectionTypeId { get; set; }

    [Required(ErrorMessage = "WaterConnectionSizeId_Required")]
    public int WaterConnectionSizeId { get; set; }

    public int? WaterConnectionStatusId { get; set; }

    private string _connectionNo = string.Empty;

    [Required(ErrorMessage = "ConnectionNo_Required")]
    [StringLength(50, ErrorMessage = "ConnectionNo_MaxLen_50")]
    public string ConnectionNo
    {
        get => _connectionNo;
        set => _connectionNo = value?.Trim() ?? string.Empty;
    }

    public string? MeterNo { get; set; }

    [Required(ErrorMessage = "ConnectionStartDate_Required")]
    public DateTime ConnectionStartDate { get; set; }

    public DateTime? ConnectionStopDate { get; set; }
}
