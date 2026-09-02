using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <inheritdoc/>
public class RVCalculationSignatureService : IRVCalculationSignatureService
{
    private readonly IRepository<RVCalculationSignatureEntity, int> _signatureRepo;

    public RVCalculationSignatureService(IRepository<RVCalculationSignatureEntity, int> signatureRepo)
    {
        _signatureRepo = signatureRepo;
    }

    public Task<RVCalculationSignatureEntity?> GetAsync(int propertyId) =>
        _signatureRepo.GetQueryable()
            .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.IsActive);

    public async Task UpsertAsync(int propertyId, string signatureHash, DateTime calculatedAt)
    {
        var existing = await _signatureRepo.GetQueryable()
            .FirstOrDefaultAsync(x => x.PropertyId == propertyId);

        if (existing != null)
        {
            existing.SignatureHash = signatureHash;
            existing.CalculatedAt = calculatedAt;
            existing.IsActive = true;
            existing.UpdatedDate = calculatedAt;
            await _signatureRepo.UpdateAsync(existing);
        }
        else
        {
            await _signatureRepo.AddAsync(new RVCalculationSignatureEntity
            {
                PropertyId = propertyId,
                SignatureHash = signatureHash,
                CalculatedAt = calculatedAt,
                IsActive = true,
                CreatedDate = calculatedAt,
                UpdatedDate = calculatedAt
            });
        }
    }
}
