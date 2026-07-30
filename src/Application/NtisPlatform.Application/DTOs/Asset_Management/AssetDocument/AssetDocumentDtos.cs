using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;

public class AssetDocumentUploadResponseDto
{
    public int DocumentId { get; set; }
    public Guid DocumentGuid { get; set; }
    public int DocumentBindingId { get; set; }
    public int AssetId { get; set; }
    public int DocumentDefinitionId { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

public class AssetDocumentDto
{
    public int DocumentId { get; set; }
    public int AssetId { get; set; }
    public int DocumentDefinitionId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }

    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}

public class AssetDocumentTypeWithStatusDto
{
    public int DocumentDefinitionId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }

    public bool HasDocument { get; set; }
    public int DocumentCount { get; set; }

    public int? DocumentId { get; set; }
    public string? Remarks { get; set; }
    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}

public class AssetDocumentGalleryDto
{
    public int AssetId { get; set; }
    public int TotalDocuments { get; set; }
    public List<AssetDocumentTypeGroupDto> DocumentTypes { get; set; } = new();
}

public class AssetDocumentTypeGroupDto
{
    public int DocumentDefinitionId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public bool HasDocument { get; set; }
    public int DocumentCount { get; set; }
    public List<AssetDocumentDto> Documents { get; set; } = new();
}

/// <summary>
/// Request DTO for POST /api/asset-documents/save-with-upload.
/// </summary>
public class AssetDocumentSaveWithUploadDto
{
    public int AssetId { get; set; }
    public int? ExistingDocumentId { get; set; }
    public int DocumentDefinitionId { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }
    public bool IsEnabled { get; set; } = true;
    public IFormFile? DocumentFile { get; set; }
}
