using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Api.DTOs;

/// <summary>
/// Form DTO for document upload (multipart/form-data binding).
/// Lives in the Presentation layer because it carries the web-framework
/// type <see cref="IFormFile"/>; the controller maps it to the
/// framework-agnostic DocumentUploadDto before invoking the application service.
/// </summary>
public class DocumentUploadFormDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public int? OwnerUserId { get; set; }

    public string? DocumentType { get; set; }

    public int? DepartmentId { get; set; }

    public int? ModuleId { get; set; }

    public string? ReferenceTableName { get; set; }

    public int? ReferenceTableId { get; set; }

    public Guid? ReferenceTableIdGuid { get; set; }

    public string? ReferencePropertyName { get; set; }

    public string? BindingPurpose { get; set; }

    public bool IsPrimaryDocument { get; set; }

    public int? AuthDepartmentId { get; set; }

    public int? AuthReferenceId { get; set; }
}
