using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RTSFieldValue;

    public class RTSFieldValueQueryParameters:BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? Id { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? ApplicationId { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? FieldDefinitionId { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Sortable]
        [Searchable]
        public string? FieldName { get; set; }

        [Filterable]
        [Sortable]
        public bool? IsActive { get; set; }
    }

