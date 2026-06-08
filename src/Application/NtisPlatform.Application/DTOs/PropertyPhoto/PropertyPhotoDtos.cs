namespace NtisPlatform.Application.DTOs.PropertyPhoto;

/// <summary>
/// Response DTO when uploading or replacing a property photo.
/// The <see cref="DocumentGuid"/> is used with the existing DocumentController endpoints:
/// GET /api/documents/{documentGuid}/view (inline / thumbnail) and
/// GET /api/documents/{documentGuid}/download.
/// </summary>
public class PropertyPhotoUploadResponseDto
{
    public int PropertyPhotoId { get; set; }
    public Guid DocumentGuid { get; set; }
    public int DocumentId { get; set; }
    public int DocumentBindingId { get; set; }
    public int PropertyId { get; set; }
    public int PhotoTypeId { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

/// <summary>
/// A single current (latest) photo for a property. Used to render the
/// "Additional Images" gallery (Front Elevation, Rear Elevation, ...).
/// A property may have multiple photos per photo type.
/// </summary>
public class PropertyPhotoDto
{
    public int PropertyPhotoId { get; set; }
    public int PropertyId { get; set; }
    public int PhotoTypeId { get; set; }
    public string PhotoTypeCode { get; set; } = string.Empty;
    public string PhotoTypeName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public string? Remarks { get; set; }

    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}

/// <summary>
/// A photo type with its current status for a property.
/// Drives the photo-slot panel / "Add Photo Plan Slot": every active photo type is returned,
/// with <see cref="PhotoCount"/> photos under it (zero, one or many) and a representative
/// (first) photo for the thumbnail.
/// </summary>
public class PropertyPhotoTypeWithStatusDto
{
    public int PhotoTypeId { get; set; }
    public string PhotoTypeCode { get; set; } = string.Empty;
    public string PhotoTypeName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }

    public bool HasPhoto { get; set; }
    public int PhotoCount { get; set; }

    // Representative (first) photo for the type, used for the thumbnail in the slot panel
    public int? PropertyPhotoId { get; set; }
    public string? Remarks { get; set; }
    public int? DocumentBindingId { get; set; }
    public Guid? DocumentGuid { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
}

/// <summary>
/// The full photo gallery for a property as a single ("whole") JSON object: every active
/// photo type, each carrying its own list of current photos (zero, one or many).
/// </summary>
public class PropertyPhotoGalleryDto
{
    public int PropertyId { get; set; }
    public int TotalPhotos { get; set; }
    public List<PropertyPhotoTypeGroupDto> PhotoTypes { get; set; } = new();
}

/// <summary>
/// One photo type within <see cref="PropertyPhotoGalleryDto"/>, with its photos nested inside.
/// </summary>
public class PropertyPhotoTypeGroupDto
{
    public int PhotoTypeId { get; set; }
    public string PhotoTypeCode { get; set; } = string.Empty;
    public string PhotoTypeName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public bool HasPhoto { get; set; }
    public int PhotoCount { get; set; }
    public List<PropertyPhotoDto> Photos { get; set; } = new();
}
