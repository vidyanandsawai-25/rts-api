using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;

/// <summary>
/// DTO for ConfigCategoryMaster
/// </summary>
public class ConfigCategoryMasterDto : BaseDtos
{
    public int CategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public int? DisplayOrder { get; set; }     
}

/// <summary>
/// DTO for creating a new ConfigCategoryMaster
/// </summary>
public class CreateConfigCategoryMasterDto: CreateBaseDtos
{
    [Required(ErrorMessage = "CategoryCode_Required")]
    [StringLength(30, ErrorMessage = "CategoryCode_MaxLen_30")]
    public string CategoryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "CategoryName_Required")]
    [StringLength(100, ErrorMessage = "CategoryName_MaxLen_100")]
    public string CategoryName { get; set; } = string.Empty;

    public int? DisplayOrder { get; set; } 
}

/// <summary>
/// DTO for updating a ConfigCategoryMaster
/// </summary>
public class UpdateConfigCategoryMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "CategoryCode_Required")]
    [StringLength(30, ErrorMessage = "CategoryCode_MaxLen_30")]
    public string CategoryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "CategoryName_Required")]
    [StringLength(100, ErrorMessage = "CategoryName_MaxLen_100")]
    public string CategoryName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; } 
}
