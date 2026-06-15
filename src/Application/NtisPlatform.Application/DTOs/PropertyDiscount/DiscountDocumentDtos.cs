using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyDiscount;

/// <summary>
/// Form DTO for uploading a discount-related document
/// </summary>
public class DiscountDocumentUploadFormDto
{
    /// <summary>
    /// The document file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// The property ID this discount document belongs to
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId must be greater than 0")]
    public int PropertyId { get; set; }

    /// <summary>
    /// The social attribute ID (discount attribute) this document is for
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "SocialAttributeId must be greater than 0")]
    public int SocialAttributeId { get; set; }

    /// <summary>
    /// Optional remark/description for the document
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// Whether the file being uploaded is a photo or a document
    /// </summary>
    public bool IsPhoto { get; set; } = false;
}

/// <summary>
/// Form DTO for replacing an existing discount document
/// </summary>
public class ReplaceDiscountDocumentFormDto
{
    /// <summary>
    /// The new document file to replace the old one
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Optional remark/description for the new document
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// Whether the file being uploaded is a photo or a document
    /// </summary>
    public bool IsPhoto { get; set; } = false;
}

/// <summary>
/// Response DTO after uploading or replacing a discount document
/// </summary>
public class DiscountDocumentUploadResponseDto
{
    public int PropertySocialDetailId { get; set; }
    public int PropertyId { get; set; }
    public int SocialAttributeId { get; set; }
    public int DocumentBindingId { get; set; }

    /// <summary>
    /// Document GUID - use with DocumentController endpoints:
    /// GET /api/documents/{documentGuid}/view
    /// GET /api/documents/{documentGuid}/download
    /// </summary>
    public Guid DocumentGuid { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string? Remark { get; set; }
}
