using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// Request DTO for splitting one old property into two new properties
/// </summary>
public class PropertySplitDto
{
    [Required(ErrorMessage = "PropertyOldId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyOldId must be greater than 0")]
    public int PropertyOldId { get; set; } = new();

    [Required(ErrorMessage = "PropertyMapId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyMapId must be greater than 0")]
    public int PropertyMapId { get; set; }

    public List<int>? PropertyId { get; set; } = new();

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "Latitude must be a valid decimal with up to 8 decimal places")]
    public string? Latitude { get; set; }

    [RegularExpression(@"^-?\d{1,3}\.\d{1,8}$", ErrorMessage = "Longitude must be a valid decimal with up to 8 decimal places")]
    public string? Longitude { get; set; }

    [MaxLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,!@#$%^&*()_+{}\[\]:;""'|\\?.~`]+$",
    ErrorMessage = "Location can only contain English letters, digits, Marathi characters, spaces, hyphens, forward slashes, and commas")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "UserId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0")]
    public int UserId { get; set; }
}
