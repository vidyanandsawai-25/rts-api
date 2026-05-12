using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterConnectionTypeDto : BaseDtos
{
    public string ConnectionTypeCode { get; set; } = string.Empty;
    public string ConnectionTypeName { get; set; } = string.Empty;
}

public class CreateWaterConnectionTypeDto : CreateBaseDtos
{
    private string _connectionTypeCode = string.Empty;
    private string _connectionTypeName = string.Empty;

    [Required(ErrorMessage = "ConnectionTypeCode_Required")]
    [StringLength(20, ErrorMessage = "ConnectionTypeCode_MaxLen_20")]
    public string ConnectionTypeCode
    {
        get => _connectionTypeCode;
        set => _connectionTypeCode = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "ConnectionTypeName_Required")]
    [StringLength(100, ErrorMessage = "ConnectionTypeName_MaxLen_100")]
    public string ConnectionTypeName
    {
        get => _connectionTypeName;
        set => _connectionTypeName = value?.Trim() ?? string.Empty;
    }
}

public class UpdateWaterConnectionTypeDto : UpdateBaseDtos
{
    private string _connectionTypeCode = string.Empty;
    private string _connectionTypeName = string.Empty;

    [Required(ErrorMessage = "ConnectionTypeCode_Required")]
    [StringLength(20, ErrorMessage = "ConnectionTypeCode_MaxLen_20")]
    public string ConnectionTypeCode
    {
        get => _connectionTypeCode;
        set => _connectionTypeCode = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "ConnectionTypeName_Required")]
    [StringLength(100, ErrorMessage = "ConnectionTypeName_MaxLen_100")]
    public string ConnectionTypeName
    {
        get => _connectionTypeName;
        set => _connectionTypeName = value?.Trim() ?? string.Empty;
    }
}
