using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.PropertyMapDetails;

public class PropertyMapDetailsQueryParameters : BaseQueryParameters
{
    public int? PropertyId { get; set; }
}
