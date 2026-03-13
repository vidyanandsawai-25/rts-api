using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster
{
    public class GrievanceCategoryQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        public int? GrievanceCategoryId { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? CategoryCode { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? CategoryName { get; set; }

        [Filterable]
        [Sortable]
        public int? DepartmentId { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? Priority { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Sortable]
        public string? EscalationLevel { get; set; }

        [Filterable]
        [Sortable]
        public bool? IsActive { get; set; }
    }
}
