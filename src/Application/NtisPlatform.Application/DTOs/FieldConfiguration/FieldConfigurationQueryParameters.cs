using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.FieldConfiguration
{
    /// <summary>
    /// Query parameters for filtering and searching field configurations
    /// </summary>
    public class FieldConfigurationQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? RulesFieldId { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? DataType { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? InputType { get; set; }

        [Filterable(FilterOperator.Equals)]
        public bool? HasApiSource { get; set; }

        [Filterable(FilterOperator.Equals)]
        public bool? HasStaticValues { get; set; }

        [Filterable(FilterOperator.Equals)]
        public bool? IsRequired { get; set; }
    }
}
