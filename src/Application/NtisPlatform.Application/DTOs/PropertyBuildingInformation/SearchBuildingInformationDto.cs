using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyBuildingInformation;

/// <summary>
/// DTO for searching building information by OldWardNo and OldSocietyName
/// Used for the POST /building-information/search API endpoint
/// </summary>
public class SearchBuildingInformationDto
{
    [Required(ErrorMessage = "OldWardNo is required.")]
    [StringLength(10, ErrorMessage = "OldWardNo cannot exceed 10 characters.")]
    public string OldWardNo { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "OldSocietyName cannot exceed 500 characters.")]
    public string? OldSocietyName { get; set; }

    public int? MapId { get; set; }
}
