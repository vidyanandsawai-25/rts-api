using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.BankMaster
{
    public class BankQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        public int? Id { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string BankCode { get; set; } = string.Empty;

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string BankName { get; set; } = string.Empty;

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string State { get; set; } = string.Empty;
    }
}
