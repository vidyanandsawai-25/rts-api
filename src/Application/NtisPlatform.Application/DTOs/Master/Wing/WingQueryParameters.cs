using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class WingQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? WingNo { get; set; }

}