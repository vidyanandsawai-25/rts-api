using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.DTOs.Master;

public class OwningDepartmentQueryParameters : BaseQueryParameters
{

    [Filterable]
    [Searchable]
    [Sortable]
    public string? OwningDepartmentName { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? Description { get; set; }

}
