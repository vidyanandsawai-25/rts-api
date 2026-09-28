using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Payload for PATCH <c>apartmentqc/{propertyId}/save-plan-type</c>.
/// <see cref="Type"/> must equal either one of the property's society's existing distinct
/// plan types (from <c>{propertyId}/plan-type</c>) or the next available plan type
/// (from <c>{propertyId}/new-plan-type</c>) — validated by the service.
/// </summary>
public class SavePlanTypeDto
{
    [Required(ErrorMessage = "ApartmentQC_PlanType_Required")]
    [StringLength(5, ErrorMessage = "ApartmentQC_PlanType_MaxLen_5")]
    public string Type { get; set; } = string.Empty;
}
