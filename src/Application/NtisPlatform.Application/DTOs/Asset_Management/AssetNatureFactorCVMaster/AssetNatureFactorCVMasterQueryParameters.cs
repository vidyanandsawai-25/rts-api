using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster
{
    public class AssetNatureFactorCVMasterQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? ConstructionTypeId { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? YearRangeCVId { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public bool? IsActive { get; set; }

        [Filterable]
        [Sortable]
        public bool? MarkedForDeletion { get; set; }
    }
}
