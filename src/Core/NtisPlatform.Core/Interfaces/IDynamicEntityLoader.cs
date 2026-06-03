using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Loads rows of an entity whose CLR type is only known at runtime, matched by a single key column.
/// This is the one piece of the bulk-update flow that genuinely needs the EF <c>DbContext</c>; it lets
/// the Application layer drive the update without referencing Entity Framework or a specific entity type.
/// </summary>
public interface IDynamicEntityLoader
{
    /// <summary>
    /// Loads every row of <paramref name="entityType"/> whose <paramref name="keyProperty"/> value is in
    /// <paramref name="keyValues"/>. Returns the rows as <see cref="BaseEntity"/>; callers read/mutate
    /// concrete properties by reflection. When <paramref name="asNoTracking"/> is false the rows are
    /// tracked by the shared context, so the caller can persist mutations via the unit of work.
    /// </summary>
    Task<IReadOnlyList<BaseEntity>> LoadByKeyAsync(
        Type entityType,
        string keyProperty,
        IReadOnlyCollection<long> keyValues,
        bool asNoTracking,
        CancellationToken cancellationToken = default);
}
