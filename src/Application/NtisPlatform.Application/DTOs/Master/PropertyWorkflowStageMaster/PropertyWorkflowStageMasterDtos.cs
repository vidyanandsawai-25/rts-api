using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;

/// <summary>
/// DTO for PropertyWorkflowStageMaster
/// </summary>
public class PropertyWorkflowStageMasterDto : BaseDtos
{
    public int Id { get; set; }
    public string? StageName { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating a new PropertyWorkflowStageMaster
/// </summary>
public class CreatePropertyWorkflowStageMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "StageName_Required")]
    [StringLength(100, ErrorMessage = "StageName_MaxLen_100")]
    public string StageName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "DisplayOrder_Required")]
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for updating a PropertyWorkflowStageMaster
/// </summary>
public class UpdatePropertyWorkflowStageMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "StageName_Required")]
    [StringLength(100, ErrorMessage = "StageName_MaxLen_100")]
    public string StageName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "DisplayOrder_Required")]
    public int DisplayOrder { get; set; }
}
