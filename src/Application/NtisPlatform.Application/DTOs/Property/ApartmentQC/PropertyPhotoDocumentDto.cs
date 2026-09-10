namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// DTO representing a single property photo document (DocumentGuid and PhotoTypeCode).
/// </summary>
public class PropertyPhotoDocumentDto
{
    public Guid DocumentGuid { get; set; }
    public string PhotoTypeCode { get; set; } = string.Empty;
}
