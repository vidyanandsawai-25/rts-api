using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertySignature;

public class PropertySignatureWardGridQueryParameters : BaseQueryParameters
{
    [Range(1, int.MaxValue)]
    public int ZoneId { get; set; }
}
