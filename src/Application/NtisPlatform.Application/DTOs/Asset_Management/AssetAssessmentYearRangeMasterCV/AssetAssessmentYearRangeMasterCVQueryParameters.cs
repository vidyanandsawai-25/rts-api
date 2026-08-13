using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;


namespace NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV
{
    public class AssetAssessmentYearRangeMasterCVQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? FromYear { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? ToYear { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public bool? IsActive { get; set; }

        [Filterable]
        [Sortable]
        public bool? MarkedForDeletion { get; set; }
    }
}
