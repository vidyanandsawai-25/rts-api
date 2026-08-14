using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Hand-rolled service for ULB document metadata rows (<c>PTIS.ULBDocument</c>), keyed by a
/// <c>PTIS.ULBDocumentType</c> code (e.g. "TAX_ZONING_DOCUMENT_LIST"). Follows the platform's
/// mandatory create-row-then-upload-then-auto-link Document/DocumentBinding pattern — see
/// <c>ULBDocumentBindingHandler</c>, which is the ONLY caller of
/// <see cref="LinkDocumentBindingAsync"/>/<see cref="UnlinkDocumentBindingAsync"/>.
/// </summary>
public interface IULBDocumentService
{
    /// <summary>
    /// Returns the current (<c>IsLatest</c>) active row for each requested document type code,
    /// WITHOUT file metadata (no dependency on <c>IDocumentApplicationService</c> here — that
    /// service resolves the registered <c>IDocumentBindingHandler</c>s, including
    /// <c>ULBDocumentBindingHandler</c>, which itself depends on this interface; joining file
    /// metadata here would be a circular dependency). Pass null/empty (or a blank string) to return
    /// every active type's latest document. Callers that need file metadata joined in should go
    /// through <see cref="IULBDocumentQueryService"/> instead, which safely depends on both this
    /// service and <c>IDocumentApplicationService</c>.
    /// </summary>
    /// <param name="typeCodes">Comma-separated document type codes, or null/blank for all types.</param>
    Task<List<ULBDocumentDto>> GetLatestAsync(string? typeCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flips any existing <c>IsLatest</c> row for the same document type to false (history is kept,
    /// not soft-deleted), inserts the new metadata row with <c>DocumentBindingId = NULL</c>, and
    /// returns its Id — the caller then uploads the actual file via the generic Document API using
    /// this Id. Throws <see cref="ArgumentException"/> if the type code doesn't exist/is inactive.
    /// </summary>
    Task<int> CreateAsync(CreateULBDocumentDto dto, CancellationToken cancellationToken = default);

    /// <summary>Called ONLY by ULBDocumentBindingHandler.OnAfterUploadAsync.</summary>
    Task LinkDocumentBindingAsync(int id, int documentBindingId, int userId, CancellationToken cancellationToken = default);

    /// <summary>Called ONLY by ULBDocumentBindingHandler.OnBeforeDeleteAsync.</summary>
    Task UnlinkDocumentBindingAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Existence check used by the binding handler's ReferenceExistsAsync.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Raw entity lookup used by the binding handler to compare DocumentBindingId.</summary>
    Task<NtisPlatform.Core.Entities.ULBDocumentEntity?> GetEntityByIdAsync(int id, CancellationToken cancellationToken = default);
}
