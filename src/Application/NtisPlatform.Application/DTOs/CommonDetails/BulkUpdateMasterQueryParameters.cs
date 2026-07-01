using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateMasterQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? UpdateCode { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? UpdateName { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? ReferenceTableName { get; set; }

    [Filterable]
    [Sortable]
    public int? DisplaySequence { get; set; }
}
