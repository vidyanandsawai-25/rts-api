namespace NtisPlatform.Application.DTOs.Asset_Management;

public class InventoryDocumentUploadResponseDto
{
    public int InventoryDocumentId { get; set; }
    public Guid DocumentGuid { get; set; }
    public int DocumentId { get; set; }
    public int DocumentBindingId { get; set; }
    public int InventoryBatchId { get; set; }
    public int DocumentTypeId { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

public class InventoryDocumentDto
{
    public int InventoryDocumentId { get; set; }
    public int InventoryBatchId { get; set; }
    public int DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }

    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}

/// <summary>
/// Request body for POST /api/inventory-documents/bulk-save.
/// Saves a list of inventory document slot metadata for a batch.
/// Returns generated IDs to use as ReferenceTableId when calling POST /api/documents/upload.
/// </summary>
public class InventoryDocumentBulkSaveDto
{
    /// <summary>The parent inventory batch ID all items belong to.</summary>
    public int InventoryBatchId { get; set; }

    /// <summary>The list of inventory document items to register/update.</summary>
    public List<InventoryDocumentItemDto> Documents { get; set; } = new();
}

/// <summary>
/// Individual item in the bulk-save request.
/// </summary>
public class InventoryDocumentItemDto
{
    /// <summary>Existing InventoryDocumentId (&gt;0 for update, null/0 for new slot).</summary>
    public int? InventoryDocumentId { get; set; }

    /// <summary>The document type ID (e.g. INVENTORY_INVOICE, INVENTORY_WARRANTY).</summary>
    public int DocumentTypeId { get; set; }

    /// <summary>Optional display/sort order within the batch.</summary>
    public int? DisplayOrder { get; set; }

    /// <summary>Optional remarks/notes for this document slot.</summary>
    public string? Remarks { get; set; }

    /// <summary>Whether this slot is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Response from POST /api/inventory-documents/bulk-save.
/// Each savedDocument.InventoryDocumentId should be passed as ReferenceTableId
/// to POST /api/documents/upload (with ReferenceTableName = "InventoryDocument").
/// </summary>
public class InventoryDocumentBulkSaveResponseDto
{
    public int InventoryBatchId { get; set; }
    public int TotalProcessed { get; set; }
    public int EnabledCount { get; set; }
    public int DisabledCount { get; set; }
    public List<InventoryDocumentDto> SavedDocuments { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

