using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

/// <summary>
/// Same category-based scoping as <see cref="PropertySearchByCategoryQueryParameters"/>
/// (Zone-wise, Ward-wise, Building-wise, or a From/To property-number range), used to select
/// which properties <see cref="ICommonDetailsService.FilterPropertiesByCategoryAsync"/> previews
/// for the given bulk-update <see cref="UpdateCode"/>.
/// </summary>
public class FilterPropertiesByCategoryRequestDto : PropertySearchByCategoryQueryParameters
{
    [Required(ErrorMessage = "CommonDetails_UpdateCode_Required")]
    [MinLength(1, ErrorMessage = "CommonDetails_UpdateCode_Required")]
    public List<string> UpdateCode { get; set; } = [];
}
