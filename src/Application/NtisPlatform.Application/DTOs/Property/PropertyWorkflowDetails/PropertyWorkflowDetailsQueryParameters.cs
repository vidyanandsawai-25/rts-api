using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;

public class PropertyWorkflowDetailsQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable]
    [Sortable]
    public int? PropertyId { get; set; }

    [Filterable]
    [Sortable]
    public int? WorkflowStageId { get; set; }

    [Filterable]
    [Sortable]
    public int? ModuleId { get; set; }

    [Filterable]
    [Sortable]
    public bool? CurrentStatus { get; set; }
}
