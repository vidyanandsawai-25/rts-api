using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Centralized service for validating entity references.
/// Checks if an entity is referenced by other entities before allowing deactivation or deletion.
/// </summary>
public class ReferenceValidationService : IReferenceValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, Func<int, CancellationToken, Task<(bool hasReferences, string errorMessage)>>> _validators;

    private static readonly Dictionary<Type, List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)>> _referenceConfig;

    static ReferenceValidationService()
    {
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<ConstructionTypeEntity>()
            .CheckReferences(
                ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.ConstructionTypeId == id)),
                ("Age Factors", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.ConstructionTypeId == id)),
                ("Nature Factors", (ctx, id) => ctx.NatureFactorCVMasters.Where(n => n.ConstructionTypeId == id)),
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.ConstructionTypeId == id))
            );
        config.ForEntity<TaxZoneEntity>()
            .CheckReferences(
                ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.TaxZoneId == id)),
                ("Properties", (ctx, id) => ctx.PropertyMast.Where(p => p.TaxZoneId == id))
            );
        _referenceConfig = config.Build();
    }

    public ReferenceValidationService(ApplicationDbContext context)
    {
        _context = context;
        _validators = new Dictionary<Type, Func<int, CancellationToken, Task<(bool, string)>>>();

        foreach (var kvp in _referenceConfig)
        {
            _validators[kvp.Key] = async (id, ct) =>
            {
                var referencingTables = new List<string>();
                foreach (var (tableName, queryFunc) in kvp.Value)
                {
                    var any = await queryFunc(_context, id).AnyAsync(ct);
                    if (any)
                        referencingTables.Add(tableName);
                }
                if (referencingTables.Any())
                {
                    var entityName = kvp.Key.Name.Replace("Entity", string.Empty);
                    return (true, $"Cannot deactivate/delete this {entityName} because it is referenced in: {string.Join(", ", referencingTables)}");
                }
                return (false, string.Empty);
            };
        }
    }

    public async Task<ValidationResult> ValidateReferencesAsync<TEntity>(int entityId, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity
    {
        var entityType = typeof(TEntity);
        if (!_validators.TryGetValue(entityType, out var validator))
        {
            // No validation configured for this entity type - allow operation
            return ValidationResult.Success();
        }
        var (hasReferences, errorMessage) = await validator(entityId, cancellationToken);
        if (hasReferences)
        {
            return ValidationResult.Failure(errorMessage);
        }
        return ValidationResult.Success();
    }
}
