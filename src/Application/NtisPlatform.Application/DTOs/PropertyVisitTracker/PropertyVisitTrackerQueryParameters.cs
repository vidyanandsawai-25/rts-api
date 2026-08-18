using System;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.PropertyVisitTracker;

public class PropertyVisitTrackerQueryParameters : BaseQueryParameters
{
    public int? UserId { get; set; }
    public int? PropertyId { get; set; }
    public int? WorkflowStageId { get; set; }
    public int? ModuleId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? FromDateTime { get; set; }
    public DateTime? ToDateTime { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
}
