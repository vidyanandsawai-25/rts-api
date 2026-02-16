using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.AssessmentYearRange
{
    public class AssessmentYearRangeQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        public int? YearId { get; set; }

        [Filterable]
        [Sortable]
        public int? FromYear { get; set; }

        [Filterable]
        [Sortable]
        public int? ToYear { get; set; }
    }
}
