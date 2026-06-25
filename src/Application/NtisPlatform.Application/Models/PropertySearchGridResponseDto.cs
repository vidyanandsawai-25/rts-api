using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Models;

public class PropertySearchGridResponseDto
{
    public PagedResult<PropertySearchResponseDto>? Results { get; set; }
    public int TotalMatchingProperties => Results?.TotalCount ?? 0;
}
