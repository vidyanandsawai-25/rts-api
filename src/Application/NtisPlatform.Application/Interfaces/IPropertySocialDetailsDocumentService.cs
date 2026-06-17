using NtisPlatform.Application.DTOs.PropertySocialDetails;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for handling document operations for PropertySocialDetails
/// </summary>
public interface IPropertySocialDetailsDocumentService
{
    /// <summary>
    /// Uploads a document for a social attribute and creates/updates PropertySocialDetails record
    /// </summary>
    Task<PropertySocialDetailsDocumentResponseDto> UploadSocialDetailsDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int socialAttributeId,
        string? remark,
        int uploadedBy,
        bool isPhoto = false,
        CancellationToken cancellationToken = default,
        bool restrictToDiscount = true);

    /// <summary>
    /// Replaces an existing document for a PropertySocialDetails record
    /// </summary>
    Task<PropertySocialDetailsDocumentResponseDto> ReplaceSocialDetailsDocumentAsync(
        int propertySocialDetailId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        string? remark,
        int uploadedBy,
        bool isPhoto = false,
        CancellationToken cancellationToken = default,
        bool restrictToDiscount = true);
}
