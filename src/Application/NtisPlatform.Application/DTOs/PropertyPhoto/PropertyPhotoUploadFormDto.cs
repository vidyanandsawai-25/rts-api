using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyPhoto;

/// <summary>
/// Form DTO for uploading a new property photo with file.
/// Used by "Add Photo Plan Slot" / adding a photo for a given photo type.
/// </summary>
public class PropertyPhotoUploadFormDto
{
    /// <summary>
    /// The image file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// The property ID this photo belongs to
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId must be greater than 0")]
    public int PropertyId { get; set; }

    /// <summary>
    /// The photo type ID (slot/category, e.g. Front Elevation)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PhotoTypeId must be greater than 0")]
    public int PhotoTypeId { get; set; }

    /// <summary>
    /// Optional gallery sort order within the property
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Optional remark / caption for the photo
    /// </summary>
    [MaxLength(500)]
    public string? Remarks { get; set; }
}

/// <summary>
/// Form DTO for replacing an existing property photo.
/// Used by the "Replace Image" button on the photo viewer.
/// </summary>
public class ReplacePropertyPhotoFormDto
{
    /// <summary>
    /// The new image file to replace the current one
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Optional remark / caption for the new photo. When omitted the previous remark is kept.
    /// </summary>
    [MaxLength(500)]
    public string? Remarks { get; set; }
}
