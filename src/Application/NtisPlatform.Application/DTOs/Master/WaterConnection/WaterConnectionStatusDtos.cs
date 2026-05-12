using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterConnectionStatusDto : BaseDtos
{
    public string StatusName { get; set; } = string.Empty;
}

public class CreateWaterConnectionStatusDto : CreateBaseDtos
{
    private string _statusName = string.Empty;

    [Required(ErrorMessage = "StatusName_Required")]
    [StringLength(100, ErrorMessage = "StatusName_MaxLen_100")]
    public string StatusName
    {
        get => _statusName;
        set => _statusName = value?.Trim() ?? string.Empty;
    }
}

public class UpdateWaterConnectionStatusDto : UpdateBaseDtos
{
    private string _statusName = string.Empty;

    [Required(ErrorMessage = "StatusName_Required")]
    [StringLength(100, ErrorMessage = "StatusName_MaxLen_100")]
    public string StatusName
    {
        get => _statusName;
        set => _statusName = value?.Trim() ?? string.Empty;
    }
}
