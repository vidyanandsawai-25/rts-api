using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding for the RTS <c>ApplicationDetails</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/> so that <c>DocumentApplicationService</c>
/// permits uploading scrutiny stage documents and citizen application attachments.
/// </summary>
public sealed class RTSApplicationDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RTSApplicationDocumentBindingHandler> _logger;

    public string ReferenceTableName => "ApplicationDetails";

    public RTSApplicationDocumentBindingHandler(
        ApplicationDbContext context,
        ILogger<RTSApplicationDocumentBindingHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "ApplicationDetails", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "RTSApplicationDetails", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "ApplicationDetail", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "AppealApplicationDetails", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> ReferenceExistsAsync(int referenceTableId, string? referencePropertyName, CancellationToken cancellationToken)
    {
        var exists = await _context.RTSApplicationDetails
            .AnyAsync(a => a.Id == referenceTableId && a.IsActive && !a.MarkedForDeletion, cancellationToken);

        if (!exists)
        {
            _logger.LogWarning(
                "RTSApplicationDocumentBindingHandler.ReferenceExistsAsync: no ApplicationDetails found with ID={AppId}.",
                referenceTableId);
        }

        return exists;
    }

    public Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
        => ReferenceExistsAsync(referenceTableId, null, cancellationToken);

    public Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RTSApplicationDocumentBindingHandler.OnAfterUploadAsync: DocumentId={DocId}, BindingId={BindingId} successfully linked to RTS ApplicationId={AppId}",
            documentId, bindingId, referenceTableId);

        return Task.CompletedTask;
    }

    public Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
