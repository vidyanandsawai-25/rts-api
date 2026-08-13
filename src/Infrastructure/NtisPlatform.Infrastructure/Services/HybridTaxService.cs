using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>Persists HYBRID strategy configuration (one row per tax) in PTIS.TaxHybridConfig.</summary>
public class HybridTaxService : IHybridTaxService
{
    private static readonly HashSet<string> ValidEvaluationPriorities = new() { "MASTER_THEN_CONDITION", "CONDITION_THEN_MASTER" };
    private static readonly HashSet<string> ValidFallbackStrategies = new() { "DEFAULT_ZERO", "CONDITION_RULE" };
    private static readonly HashSet<string> ValidResultBases = new() { "NONE", "RV", "ALV" };

    private readonly ApplicationDbContext _context;

    public HybridTaxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TaxHybridConfigDto> GetConfigAsync(int taxId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TaxHybridConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TaxId == taxId, cancellationToken);

        if (entity is null)
        {
            return new TaxHybridConfigDto { TaxId = taxId };
        }

        return new TaxHybridConfigDto
        {
            TaxId = entity.TaxId,
            EvaluationPriority = entity.EvaluationPriority,
            FallbackStrategy = entity.FallbackStrategy,
            ResultBase = entity.ResultBase
        };
    }

    public async Task<TaxHybridConfigDto> SaveConfigAsync(TaxHybridConfigDto config, CancellationToken cancellationToken = default)
    {
        if (!ValidEvaluationPriorities.Contains(config.EvaluationPriority))
        {
            throw new ArgumentException($"Invalid EvaluationPriority '{config.EvaluationPriority}'. Must be one of: {string.Join(", ", ValidEvaluationPriorities)}.");
        }
        if (!ValidFallbackStrategies.Contains(config.FallbackStrategy))
        {
            throw new ArgumentException($"Invalid FallbackStrategy '{config.FallbackStrategy}'. Must be one of: {string.Join(", ", ValidFallbackStrategies)}.");
        }
        if (!ValidResultBases.Contains(config.ResultBase))
        {
            throw new ArgumentException($"Invalid ResultBase '{config.ResultBase}'. Must be one of: {string.Join(", ", ValidResultBases)}.");
        }

        var taxExists = await _context.TaxMaster.AnyAsync(t => t.Id == config.TaxId, cancellationToken);
        if (!taxExists)
        {
            throw new ArgumentException($"TaxId={config.TaxId} does not exist.");
        }

        var entity = await _context.TaxHybridConfigs
            .FirstOrDefaultAsync(c => c.TaxId == config.TaxId, cancellationToken);

        if (entity is null)
        {
            entity = new TaxHybridConfigEntity
            {
                TaxId = config.TaxId,
                IsActive = true,
                CreatedBy = config.UpdatedBy,
                CreatedDate = DateTime.UtcNow
            };
            _context.TaxHybridConfigs.Add(entity);
        }
        else
        {
            entity.UpdatedBy = config.UpdatedBy;
            entity.UpdatedDate = DateTime.UtcNow;
        }

        entity.EvaluationPriority = config.EvaluationPriority;
        entity.FallbackStrategy = config.FallbackStrategy;
        entity.ResultBase = config.ResultBase;

        await _context.SaveChangesAsync(cancellationToken);

        config.EvaluationPriority = entity.EvaluationPriority;
        config.FallbackStrategy = entity.FallbackStrategy;
        config.ResultBase = entity.ResultBase;
        return config;
    }
}
