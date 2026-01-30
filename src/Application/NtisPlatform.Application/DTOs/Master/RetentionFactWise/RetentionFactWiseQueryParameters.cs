using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class RetentionFactWiseQueryParameters : BaseQueryParameters
{  
    [Filterable]
    [Sortable]
    public double? FactorValue { get; set; }

    [Filterable]
    [Sortable]
    public double? FromFactor { get; set; }

    [Filterable]
    [Sortable]
    public double? ToFactor { get; set; }
}
