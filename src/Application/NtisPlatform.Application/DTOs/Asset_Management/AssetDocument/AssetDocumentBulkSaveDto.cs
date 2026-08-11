using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;

/// <summary>
/// DTO for bulk save of all asset documents with a single Save button.
/// </summary>
public class AssetDocumentBulkSaveDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "AssetId must be greater than 0")]
    public int AssetId { get; set; }

    [Required]
    public List<AssetDocumentItemDto> Documents { get; set; } = new();
}

public class AssetDocumentItemDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DocumentDefinitionId must be greater than 0")]
    public int DocumentDefinitionId { get; set; }

    public bool IsEnabled { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "DisplayOrder cannot be negative")]
    public int? DisplayOrder { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public int? ExistingDocumentId { get; set; }

    public Guid? ExistingDocumentGuid { get; set; }
}

public class AssetDocumentBulkSaveResponseDto
{
    public int AssetId { get; set; }
    public int TotalProcessed { get; set; }
    public int EnabledCount { get; set; }
    public int DisabledCount { get; set; }

    public List<AssetDocumentTypeWithStatusDto> UpdatedDocumentTypes { get; set; } = new();

    public List<string> Errors { get; set; } = new();
}
