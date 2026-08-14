using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Read-side orchestration for <c>ULBDocumentController.GetLatest</c>: fetches the latest ULB
/// document rows via <see cref="IULBDocumentService"/> and joins in file metadata via
/// <see cref="IDocumentApplicationService"/>.
/// <para>
/// Deliberately a SEPARATE service from <c>ULBDocumentService</c> — nothing depends on this one
/// (unlike <c>IULBDocumentService</c>, which <c>ULBDocumentBindingHandler</c> depends on), so it
/// can safely depend on <c>IDocumentApplicationService</c> without recreating the circular
/// dependency that <c>ULBDocumentService</c> deliberately avoids
/// (<c>IDocumentApplicationService</c> → all <c>IDocumentBindingHandler</c>s →
/// <c>ULBDocumentBindingHandler</c> → <c>IULBDocumentService</c>).
/// </para>
/// </summary>
public interface IULBDocumentQueryService
{
    /// <param name="typeCodes">Comma-separated document type codes, or null/blank for all types.</param>
    Task<List<ULBDocumentDto>> GetLatestAsync(string? typeCodes, CancellationToken cancellationToken = default);
}
