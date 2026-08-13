using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// CRUD for the <c>PTIS.ULBDocumentType</c> master (document-category lookup consumed by
/// <c>IULBDocumentService</c>). Kept as a separate service/controller from <c>ULBDocumentController</c>
/// so managing the type catalog (adding a new category, renaming, deactivating) is independent of
/// uploading/viewing/deleting the documents themselves.
/// </summary>
public interface IULBDocumentTypeService
{
    Task<List<ULBDocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ULBDocumentTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="ArgumentException"/> if the code already exists.</summary>
    Task<int> CreateAsync(CreateULBDocumentTypeDto dto, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no row with <paramref name="id"/> exists.</summary>
    Task<bool> UpdateAsync(int id, UpdateULBDocumentTypeDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the type (<c>IsActive = false</c>) rather than hard-deleting — existing
    /// <c>ULBDocument</c> rows keep a valid FK, and <c>ULBDocumentService.CreateAsync</c> already
    /// rejects inactive type codes.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
