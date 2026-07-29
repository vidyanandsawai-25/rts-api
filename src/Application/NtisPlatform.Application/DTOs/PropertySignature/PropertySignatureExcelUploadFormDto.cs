using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertySignature;

public class PropertySignatureExcelUploadFormDto
{
    [Range(1, int.MaxValue, ErrorMessage = "SignAuthorityId is required")]
    public int SignAuthorityId { get; set; }

    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;
}
