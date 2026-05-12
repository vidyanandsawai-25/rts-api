using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterConnectionSizeDto : BaseDtos
{
    public decimal ConnectionSize { get; set; }
    public string ConnectionSizeUnit { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
}

public class CreateWaterConnectionSizeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ConnectionSize_Required")]
    [Range(0.01, 9999.99, ErrorMessage = "ConnectionSize_Range")]
    public decimal ConnectionSize { get; set; }

    private string _connectionSizeUnit = string.Empty;

    [Required(ErrorMessage = "ConnectionSizeUnit_Required")]
    [StringLength(20, ErrorMessage = "ConnectionSizeUnit_MaxLen_20")]
    public string ConnectionSizeUnit
    {
        get => _connectionSizeUnit;
        set => _connectionSizeUnit = value?.Trim() ?? string.Empty;
    }
}

public class UpdateWaterConnectionSizeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ConnectionSize_Required")]
    [Range(0.01, 9999.99, ErrorMessage = "ConnectionSize_Range")]
    public decimal ConnectionSize { get; set; }

    private string _connectionSizeUnit = string.Empty;

    [Required(ErrorMessage = "ConnectionSizeUnit_Required")]
    [StringLength(20, ErrorMessage = "ConnectionSizeUnit_MaxLen_20")]
    public string ConnectionSizeUnit
    {
        get => _connectionSizeUnit;
        set => _connectionSizeUnit = value?.Trim() ?? string.Empty;
    }
}
