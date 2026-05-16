using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master;

public class ScreenFormFieldMasterQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? ScreenId { get; set; }

    [Filterable]
    [Sortable]
    public int? SectionId { get; set; }

    [Filterable]
    [Sortable]
    public int? DropdownSourceId { get; set; }

    [Filterable]
    [Sortable]
    public int? ParentFieldId { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Searchable]
    [Sortable]
    public string? FieldName { get; set; }

    [Searchable]
    [Sortable]
    public string? FieldLabel { get; set; }

    [Searchable]
    [Sortable]
    public string? FieldCode { get; set; }
}