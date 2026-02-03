using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.YearMaster
{
    public class YearMasterQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? Year { get; set; }
    }
}
