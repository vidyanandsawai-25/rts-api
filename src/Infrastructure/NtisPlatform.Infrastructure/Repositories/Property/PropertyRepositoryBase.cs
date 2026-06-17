using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Shared Infrastructure base class for all per-tab property repositories.
/// <para>
/// Implements <see cref="IPropertyAggregateRepository"/> so every concrete adapter that extends
/// this class automatically satisfies the aggregate-root persistence port without any additional
/// code — the single tracked-entity query is written here once and re-used across all tab
/// repositories. This eliminates the duplicate <c>GetActivePropertyAsync</c> implementations
/// that previously appeared in each per-tab repository class.
/// </para>
/// <para>
/// The <c>_context</c> field is <c>protected</c> so inheriting repositories can reach all
/// DbSets they need for tab-specific queries without re-declaring the dependency.
/// </para>
/// </summary>
public abstract class PropertyRepositoryBase : IPropertyAggregateRepository
{
    protected readonly ApplicationDbContext _context;

    protected PropertyRepositoryBase(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the tracked <see cref="PropertyEntity"/> required for the write path,
    /// or <c>null</c> when the property is not found / inactive / soft-deleted.
    /// Tracked (no <c>AsNoTracking</c>) so callers can mutate fields and persist via
    /// <see cref="NtisPlatform.Core.Interfaces.IUnitOfWork"/>.
    /// </summary>
    public virtual Task<PropertyEntity?> GetActivePropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
        => _context.PropertyMast
            .FirstOrDefaultAsync(
                p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion,
                cancellationToken);
}
