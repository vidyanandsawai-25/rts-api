using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.ConfigValueMaster;

/// <summary>
/// DTO for ConfigValueMaster
/// </summary>
public class ConfigValueMasterDto : BaseDtos
{
    public int ConfigValueId { get; set; }
    public int ConfigKeyId { get; set; }
    public int? DepartmentId { get; set; }
    public int? ModuleId { get; set; }
    public string? Value { get; set; }
}

/// <summary>
/// DTO for creating a new ConfigValueMaster
/// </summary>
public class CreateConfigValueMasterDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ConfigKeyId_Required")]
    public int ConfigKeyId { get; set; }

    public int? DepartmentId { get; set; }

    public int? ModuleId { get; set; }

    [StringLength(500, ErrorMessage = "Value_MaxLen_500")]
    public string? Value { get; set; }
}

/// <summary>
/// DTO for updating a ConfigValueMaster
/// </summary>
public class UpdateConfigValueMasterDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ConfigKeyId_Required")]
    public int ConfigKeyId { get; set;}

    public int? DepartmentId { get; set; }

    public int? ModuleId { get; set; }

    [StringLength(500, ErrorMessage = "Value_MaxLen_500")]
    public string? Value { get; set; }
}
