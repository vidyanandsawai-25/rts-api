using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateFieldConfigQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? BulkUpdateMasterId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? FieldName { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? DisplayName { get; set; }

    [Filterable]
    [Sortable]
    public string? ControlType { get; set; }

    [Filterable]
    [Sortable]
    public string? DataType { get; set; }

    [Filterable]
    [Sortable]
    public int? SequenceNo { get; set; }
}
