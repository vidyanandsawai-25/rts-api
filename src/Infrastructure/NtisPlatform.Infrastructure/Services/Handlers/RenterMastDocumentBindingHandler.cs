using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Handlers;

public sealed class RenterMastDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly ApplicationDbContext _context;

    public string ReferenceTableName => "RenterMast";

    public RenterMastDocumentBindingHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "RenterMast", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> ReferenceExistsAsync(int referenceTableId, string? referencePropertyName, CancellationToken cancellationToken)
    {
        // Allow linking when referencing either an existing RenterMast row
        // OR the parent PropertyDetails (floor details) row before RenterMast is created.
        var renterExists = await _context.RenterMast.AnyAsync(r => r.Id == referenceTableId && r.IsActive && !r.MarkedForDeletion, cancellationToken);
        if (renterExists) return true;

        return await _context.PropertyDetails.AnyAsync(p => p.Id == referenceTableId && p.IsActive, cancellationToken);
    }

    public async Task OnAfterUploadAsync(int documentId, int bindingId, int referenceTableId, int uploadedBy, CancellationToken cancellationToken)
    {
        var renter = await _context.RenterMast.FindAsync(new object[] { referenceTableId }, cancellationToken);
        if (renter != null)
        {
            renter.DocumentBindingId = bindingId;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnBeforeDeleteAsync(DocumentBindingEntity binding, int deletedBy, CancellationToken cancellationToken)
    {
        var renter = await _context.RenterMast.FirstOrDefaultAsync(r => r.DocumentBindingId == binding.Id, cancellationToken);
        if (renter != null)
        {
            renter.DocumentBindingId = null;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
