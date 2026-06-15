using NtisPlatform.Application.DTOs.PropertyDiscount;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for handling document operations for discount-related PropertySocialDetails
/// </summary>
public interface IPropertyDiscountDocumentService
{
    /// <summary>
    /// Uploads a document for a discount attribute and creates/updates PropertySocialDetails record
    /// </summary>
    Task<DiscountDocumentUploadResponseDto> UploadDiscountDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int socialAttributeId,
        string? remark,
        int uploadedBy,
        bool isPhoto = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing discount document for a PropertySocialDetails record
    /// </summary>
    Task<DiscountDocumentUploadResponseDto> ReplaceDiscountDocumentAsync(
        int propertySocialDetailId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        string? remark,
        int uploadedBy,
        bool isPhoto = false,
        CancellationToken cancellationToken = default);
}
