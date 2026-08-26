using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RTSApplication;

public class RTSApplicationQueryParameters:BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int DepartmentId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int ServiceId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? ApplicationNo { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? ApplicationStatus { get; set; }
    [Sortable]

    public DateTime? CreatedDate { get; set; }
    [Sortable]
    [Searchable]
    public string? ApplicantName { get; set; }
    [Sortable]
    public DateTime? UpdatedDate { get; set; }


}
