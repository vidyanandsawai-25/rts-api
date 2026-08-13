using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.PropertySocialDetails;

public class PropertySocialDetailsQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? PropertyId { get; set; }

    [Filterable]
    [Sortable]
    public int? SocialAttributeId { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    public bool? MarkedForDeletion { get; set; }
}