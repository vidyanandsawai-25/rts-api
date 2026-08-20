using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>PropertyCertificate</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/> so that
/// <c>DocumentApplicationService</c> remains fully ignorant of certificate-specific logic.
///
/// <para>
/// Responsibilities:
/// <list type="bullet">
///   <item>After upload — link the new <c>DocumentBindingId</c> back to the <c>PropertyCertificate</c> row.</item>
///   <item>Before delete — unlink the document binding from the <c>PropertyCertificate</c> row.</item>
/// </list>
/// </para>
/// </summary>
public sealed class PropertyCertificateDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IPropertyCertificateService _certificateService;
    private readonly ILogger<PropertyCertificateDocumentBindingHandler> _logger;

    /// <inheritdoc/>
    public string ReferenceTableName => "PropertyCertificate";

    public PropertyCertificateDocumentBindingHandler(
        IPropertyCertificateService certificateService,
        ILogger<PropertyCertificateDocumentBindingHandler> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "PropertyCertificate", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "PropertyCertificates", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
    {
        var certificate = await _certificateService.GetByIdAsync(
            referenceTableId,
            PropertyCertificateIncludeOptions.None,
            cancellationToken);

        if (certificate == null)
        {
            _logger.LogWarning(
                "PropertyCertificateDocumentBindingHandler.ReferenceExistsAsync: no PropertyCertificate found with ID={CertId}.",
                referenceTableId);
        }

        return certificate != null;
    }

    /// <summary>
    /// Links the newly created <c>DocumentBindingId</c> back to the <c>PropertyCertificate</c> row
    /// identified by <paramref name="referenceTableId"/>. The row is guaranteed to exist at this
    /// point because <see cref="ReferenceExistsAsync"/> is checked before the transaction/file
    /// write even started.
    /// </summary>
    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "PropertyCertificateDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to CertificateId={CertId}",
            bindingId, referenceTableId);

        await _certificateService.UpdateDocumentBindingAsync(
            referenceTableId,
            bindingId,
            uploadedBy,
            cancellationToken);
    }

    /// <summary>
    /// Unlinks the document binding from the <c>PropertyCertificate</c> row
    /// when its associated document is being deleted.
    /// </summary>
    public Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken)
    {
        // DocumentBindingId is intentionally kept intact on the PropertyCertificate row
        // when soft-deleting documents so all original metadata columns remain untouched.
        return Task.CompletedTask;
    }
}
