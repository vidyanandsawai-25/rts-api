using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs
{
    public class RetentionYearWiseQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        public double? FactorValue { get; set; }

        [Filterable]
        [Sortable]
        public int? FromYear { get; set; }

        [Filterable]
        [Sortable]
        public int? ToYear { get; set; }
    }
}
