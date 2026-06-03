using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyCertificate;

/// <summary>
/// Form DTO for replacing PropertyCertificate document
/// </summary>
public class ReplacePropertyCertificateDocumentFormDto
{
    /// <summary>
    /// The new certificate file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;
}
