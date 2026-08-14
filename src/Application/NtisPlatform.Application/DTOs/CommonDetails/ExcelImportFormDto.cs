using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

/// <summary>
/// Form DTO for the bulk-update Excel upload. Carries the update type and the edited spreadsheet.
/// </summary>
public class ExcelImportFormDto
{
    [Required(ErrorMessage = "UpdateCode is required")]
    public string UpdateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;

    public string? Remarks { get; set; }
}
