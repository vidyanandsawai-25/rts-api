using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

/// <summary>
/// DTO for bulk save of all asset photos with a single Save button.
/// Allows enabling/disabling multiple photo types and saving metadata all at once.
/// Documents are uploaded separately via the upload/replace endpoints.
/// </summary>
public class AssetPhotoBulkSaveDto
{
    /// <summary>
    /// Asset ID from AMS.AssetMaster
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "AssetId must be greater than 0")]
    public int AssetId { get; set; }

    /// <summary>
    /// List of all photo type items with their enable/disable state and metadata
    /// </summary>
    [Required]
    public List<AssetPhotoItemDto> Photos { get; set; } = new();
}

/// <summary>
/// Individual photo item for bulk save.
/// Represents one photo type card in the UI.
/// </summary>
public class AssetPhotoItemDto
{
    /// <summary>
    /// Photo Type ID from AMS.AssetPhotoType master table
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PhotoTypeId must be greater than 0")]
    public int PhotoTypeId { get; set; }

    /// <summary>
    /// Whether this photo type is enabled (toggle in UI)
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Optional display order for the photo
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "DisplayOrder cannot be negative")]
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Optional remarks / caption for the photo
    /// </summary>
    [MaxLength(500)]
    public string? Remarks { get; set; }

    /// <summary>
    /// Existing AssetPhoto record ID (null for new, > 0 for existing)
    /// </summary>
    public int? ExistingPhotoId { get; set; }

    /// <summary>
    /// Existing Document GUID (for existing photos with documents)
    /// </summary>
    public Guid? ExistingDocumentGuid { get; set; }
}

/// <summary>
/// Response DTO for the bulk save operation
/// </summary>
public class AssetPhotoBulkSaveResponseDto
{
    public int AssetId { get; set; }
    public int TotalProcessed { get; set; }
    public int EnabledCount { get; set; }
    public int DisabledCount { get; set; }

    /// <summary>
    /// Updated photo-types-with-status after save, ready to refresh the UI
    /// </summary>
    public List<AssetPhotoTypeWithStatusDto> UpdatedPhotoTypes { get; set; } = new();

    /// <summary>
    /// Per-item errors (non-fatal). If any, Success = false is returned.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
