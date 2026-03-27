using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.DTOs.Master.PaymentMode;

public class PaymentModeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public int ? PaymentModeId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string Code { get; set; } = string.Empty;

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string Type { get; set; } = string.Empty;
}
