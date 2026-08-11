using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management
{
    public class AssetGrievanceRemarkQueryParameters : BaseQueryParameters
    {
        
        [Filterable]
        [Sortable]
        public int? GrievanceCategoryId { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? Remark { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        public string? Description { get; set; }

        [Filterable]
        [Sortable]
        public bool? IsActive { get; set; } = true;

        [Filterable]
        [Sortable]
        public bool? MarkedForDeletion { get; set; } = false;
    }
}
