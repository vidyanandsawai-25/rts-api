using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.OldSociety;

/// <summary>
/// DTO for Old Society search query parameters
/// </summary>
public class SearchOldSocietyDto
{
    /// <summary>
    /// Ward Number (required)
    /// </summary>
    [Required(ErrorMessage = "WardNo is required")]
    [RegularExpression(@"^[\u0900-\u097FA-Za-z0-9\s\-/,]+$",
        ErrorMessage = "WardNo can only contain English letters, digits, Marathi characters, spaces, hyphens, forward slashes, and commas")]
    public string WardNo { get; set; } = string.Empty;

    /// <summary>
    /// Optional search by society name, address, wing, or flat/shop number
    /// </summary>
    [StringLength(
        200,
        ErrorMessage = "SearchText cannot exceed 200 characters")]
    public string? SearchText { get; set; }

    /// <summary>
    /// Page number starting from 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of records per page.
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// DTO for Old Society response data
/// </summary>
public class OldSocietyDto
{
    /// <summary>
    /// Old Society Name
    /// </summary>
    public string? OldSocietyName { get; set; }

    /// <summary>
    /// Old Address (Marathi)
    /// </summary>
    public string? OldAddress { get; set; }

    /// <summary>
    /// Comma-separated list of all wings in the society
    /// </summary>
    public string? Wings { get; set; }

    /// <summary>
    /// Total distinct wings count
    /// </summary>
    public int TotalWing { get; set; }

    /// <summary>
    /// Total distinct flats/shops count
    /// </summary>
    public int TotalFlatOrShop { get; set; }
}

/// <summary>
/// Response DTO for Old Society list
/// </summary>
public class OldSocietyResponseDto
{
    public List<OldSocietyDto> Data { get; set; } = new();

    /// <summary>
    /// Total number of unique societies.
    /// </summary>
    public int Count { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public bool HasNext { get; set; }

    public bool HasPrevious { get; set; }
}
