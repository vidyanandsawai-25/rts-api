using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
namespace NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster
{

    public class RuleEffectTypeQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        public string? EffectType { get; set; }
        [Filterable(FilterOperator.Equals)]
        public bool? IsActive { get; set; }
    }
}