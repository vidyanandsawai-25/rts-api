using Microsoft.AspNetCore.Http;
using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.LockUnlock;

/// <summary>
/// Form DTO for searching property locks by uploading an Excel file.
/// Extends <see cref="BaseQueryParameters"/> for platform-standard pagination (PageNumber, PageSize) and search filtering (SearchTerm).
/// </summary>
public class SearchByExcelFileRequestDto : BaseQueryParameters
{
    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;
}
