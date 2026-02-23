using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

    public class AssessmentYearRangeCVQueryParameters : BaseQueryParameters  
    {
        [Filterable]
        [Sortable]
        public int? YearRangeId { get; set; }

        [Filterable]
        [Sortable]
        public int? FromYear { get; set; }

        [Filterable]
        [Sortable]
        public int? ToYear { get; set; }
    }

