using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Property;

public class MappedOldPropertyQueryParameters : BaseQueryParameters
{
    public int PropertyId { get; set; }
}