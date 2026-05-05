using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
namespace NtisPlatform.Application.DTOs.Master.RuleScopeMaster
{

    public class RuleScopeQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        public string? RuleScope { get; set; }
        [Filterable(FilterOperator.Equals)]
        public bool? IsActive { get; set; }
    }
}