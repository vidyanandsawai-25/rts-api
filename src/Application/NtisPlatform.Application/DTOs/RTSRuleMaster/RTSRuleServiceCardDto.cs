using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.RTSRuleMaster;

public class RTSRuleServiceCardQueryParameters : BaseQueryParameters
{
    [Required]
    [StringLength(100)]
    public string RuleGroup { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }
    [Range(1, int.MaxValue)]
    public int? ServiceId { get; set; }
}

public class RTSRuleServiceCardDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string RuleGroup { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public int ActiveRuleCount { get; set; }
}
