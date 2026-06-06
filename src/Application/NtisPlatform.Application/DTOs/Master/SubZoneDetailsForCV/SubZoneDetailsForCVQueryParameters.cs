using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class SubZoneDetailsForCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? MoujaId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? SubZoneNo { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? SubZoneName { get; set; }
}
