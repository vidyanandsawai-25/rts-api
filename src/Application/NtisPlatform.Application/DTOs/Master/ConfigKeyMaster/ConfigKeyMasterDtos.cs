using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;

/// <summary>
/// DTO for ConfigKeyMaster
/// </summary>
public class ConfigKeyMasterDto : BaseDtos
{
    public int ConfigKeyId { get; set; }
    public int? CategoryId { get; set; }
    public string? ConfigCode { get; set; }
    public string? ConfigName { get; set; }
    public string? Description { get; set; }
    public string? DataType { get; set; }
    public string? ControlType { get; set; }
    public string? DefaultValue { get; set; }     
}

/// <summary>
/// DTO for creating a new ConfigKeyMaster
/// </summary>
public class CreateConfigKeyMasterDto : CreateBaseDtos
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "ConfigCode_Required")]
    [StringLength(60, ErrorMessage = "ConfigCode_MaxLen_60")]
    public string ConfigCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ConfigName_Required")]
    [StringLength(150, ErrorMessage = "ConfigName_MaxLen_150")]
    public string ConfigName { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Description_MaxLen_400")]
    public string? Description { get; set; }

    [StringLength(20, ErrorMessage = "DataType_MaxLen_20")]
    public string? DataType { get; set; }

    [StringLength(30, ErrorMessage = "ControlType_MaxLen_30")]
    public string? ControlType { get; set; }

    [StringLength(500, ErrorMessage = "DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }
 
}

/// <summary>
/// DTO for updating a ConfigKeyMaster
/// </summary>
public class UpdateConfigKeyMasterDto : UpdateBaseDtos
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "ConfigCode_Required")]
    [StringLength(60, ErrorMessage = "ConfigCode_MaxLen_60")]
    public string ConfigCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "ConfigName_Required")]
    [StringLength(150, ErrorMessage = "ConfigName_MaxLen_150")]
    public string ConfigName { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Description_MaxLen_400")]
    public string? Description { get; set; }

    [StringLength(20, ErrorMessage = "DataType_MaxLen_20")]
    public string? DataType { get; set; }

    [StringLength(30, ErrorMessage = "ControlType_MaxLen_30")]
    public string? ControlType { get; set; }

    [StringLength(500, ErrorMessage = "DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }
 
}
