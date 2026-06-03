using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyCertificate;

/// <summary>
/// DTO for bulk save of all property certificates with single save button
/// Matches UI where user can enable/disable multiple certificates and save all at once
/// </summary>
public class PropertyCertificateBulkSaveDto
{
    /// <summary>
    /// Property ID
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId must be greater than 0")]
    public int PropertyId { get; set; }

    /// <summary>
    /// List of all certificates with their status and data
    /// </summary>
    [Required]
    public List<PropertyCertificateItemDto> Certificates { get; set; } = new();
}

/// <summary>
/// Individual certificate item for bulk save
/// Represents one certificate card in the UI
/// </summary>
public class PropertyCertificateItemDto
{
    /// <summary>
    /// Certificate Type ID from master table
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CertificateTypeId must be greater than 0")]
    public int CertificateTypeId { get; set; }

    /// <summary>
    /// Whether this certificate is enabled (toggle in UI)
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Certificate Number (from input field)
    /// </summary>
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Certificate Date (from date picker)
    /// </summary>
    public DateTime? CertificateDate { get; set; }

    /// <summary>
    /// Existing PropertyCertificate ID (null for new, >0 for existing)
    /// </summary>
    public int? PropertyCertificateId { get; set; }

    /// <summary>
    /// Existing Document GUID (for existing certificates with documents)
    /// </summary>
    public Guid? ExistingDocumentGuid { get; set; }

    /// <summary>
    /// Flag to indicate if user wants to upload new document
    /// </summary>
    public bool HasNewDocument { get; set; }
}

/// <summary>
/// Response DTO for bulk save operation
/// </summary>
public class PropertyCertificateBulkSaveResponseDto
{
    public int PropertyId { get; set; }
    public int TotalProcessed { get; set; }
    public int EnabledCount { get; set; }
    public int DisabledCount { get; set; }
    public List<PropertyCertificateWithStatusDto> UpdatedCertificates { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
