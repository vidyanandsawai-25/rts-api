using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyMapDetails;

public class PropertyMapDetailsQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "PropertyMapDetails_CreatedBy_required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMapDetails_CreatedBy_invalid")]
    public int CreatedBy { get; set; }

    [Required(ErrorMessage = "PropertyMapDetails_PropertyId_required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMapDetails_PropertyId_invalid")]
    public int PropertyId { get; set; }
    public int? SocietyId { get; set; }
}
