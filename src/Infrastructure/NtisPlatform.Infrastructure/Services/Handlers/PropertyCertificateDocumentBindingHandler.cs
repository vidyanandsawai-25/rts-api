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

    /// <summary>
    /// Links the newly created <c>DocumentBindingId</c> back to the <c>PropertyCertificate</c> row
    /// identified by <paramref name="referenceTableId"/>.
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

        // Validate the certificate exists before linking
        var certificate = await _certificateService.GetByIdAsync(
            referenceTableId,
            PropertyCertificateIncludeOptions.None,
            cancellationToken);

        if (certificate == null)
        {
            throw new ArgumentException(
                $"Property certificate with ID {referenceTableId} not found.",
                nameof(referenceTableId));
        }

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
    public async Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken)
    {
        if (!binding.ReferenceTableId.HasValue || binding.ReferenceTableId.Value <= 0)
            return;

        _logger.LogDebug(
            "PropertyCertificateDocumentBindingHandler.OnBeforeDeleteAsync: unlinking CertificateId={CertId}",
            binding.ReferenceTableId.Value);

        // Verify the certificate's binding ID still points to this binding before unlinking
        var certificate = await _certificateService.GetByIdAsync(
            binding.ReferenceTableId.Value,
            PropertyCertificateIncludeOptions.None,
            cancellationToken);

        if (certificate != null && certificate.DocumentBindingId == binding.Id)
        {
            await _certificateService.UnlinkDocumentBindingAsync(
                binding.ReferenceTableId.Value,
                deletedBy,
                cancellationToken);
        }
    }
}
