using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;

public class PropertyWorkflowDetailsDto : BaseDtos
{
    public int PropertyId { get; set; }
    public int WorkflowStageId { get; set; }
    public int? ModuleId { get; set; }
    public bool? CurrentStatus { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreatePropertyWorkflowDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyWorkflowDetails_PropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyWorkflowDetails_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertyWorkflowDetails_WorkflowStageId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyWorkflowDetails_WorkflowStageId_Invalid")]
    public int WorkflowStageId { get; set; }

    public int? ModuleId { get; set; }
}

public class UpdatePropertyWorkflowDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyWorkflowDetails_WorkflowStageId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyWorkflowDetails_WorkflowStageId_Invalid")]
    public int WorkflowStageId { get; set; }

    public int? ModuleId { get; set; }
    public bool? CurrentStatus { get; set; }
}
