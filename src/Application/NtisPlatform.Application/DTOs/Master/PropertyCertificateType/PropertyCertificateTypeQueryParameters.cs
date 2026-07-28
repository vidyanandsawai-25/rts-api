using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.PropertyCertificateType;

public class PropertyCertificateTypeQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? Id { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? CertificateTypeName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? CertificateTypeCode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsProtected { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsRequired { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? FieldCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? SectionCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? DocumentTypeCode { get; set; }
}
