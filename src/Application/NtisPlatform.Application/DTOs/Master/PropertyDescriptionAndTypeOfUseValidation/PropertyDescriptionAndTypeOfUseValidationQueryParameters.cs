using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;

public class PropertyDescriptionAndTypeOfUseValidationQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public int? PropertyTypeId { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public int? TypeOfUseId { get; set; }
}
