using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.SocialAttributeMaster
{
    public class SocialAttributeMasterQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        [Sortable]
        [Searchable]
        public string? SocialAttributeCode { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Sortable]
        [Searchable]
        public string? SocialAttributeName { get; set; }

        [Filterable]
        public bool? IsRequiredWhenParentTrue { get; set; }

        [Filterable]
        public bool? IsDiscountApplicable { get; set; }

        [Filterable]
        public bool? IsActive { get; set; }
    }
}
