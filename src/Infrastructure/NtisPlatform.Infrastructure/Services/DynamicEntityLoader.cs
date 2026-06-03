using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// EF Core implementation of <see cref="IDynamicEntityLoader"/>. Resolves the typed <c>DbSet</c> for a
/// runtime entity type and queries it by a key column. Contains no table-name or entity-name literals —
/// the caller supplies the entity type and key column (from the Application-layer registry).
/// </summary>
public class DynamicEntityLoader : IDynamicEntityLoader
{
    private static readonly MethodInfo LoadTypedMethod =
        typeof(DynamicEntityLoader).GetMethod(nameof(LoadTypedAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ApplicationDbContext _context;

    public DynamicEntityLoader(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<BaseEntity>> LoadByKeyAsync(
        Type entityType,
        string keyProperty,
        IReadOnlyCollection<long> keyValues,
        bool asNoTracking,
        CancellationToken cancellationToken = default)
    {
        // Dispatch to the strongly-typed loader so EF can build a proper DbSet<T> query.
        var typed = LoadTypedMethod.MakeGenericMethod(entityType);
        return (Task<IReadOnlyList<BaseEntity>>)typed.Invoke(
            this, new object[] { keyProperty, keyValues, asNoTracking, cancellationToken })!;
    }

    private async Task<IReadOnlyList<BaseEntity>> LoadTypedAsync<T>(
        string keyProperty,
        IReadOnlyCollection<long> keyValues,
        bool asNoTracking,
        CancellationToken cancellationToken) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>();
        if (asNoTracking)
            query = query.AsNoTracking();

        query = query.Where(BuildKeyPredicate<T>(keyProperty, keyValues));

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Cast<BaseEntity>().ToList();
    }

    /// <summary>
    /// Builds <c>x => keyValues.Contains(x.[keyProperty])</c>, coercing the supplied <see cref="long"/>
    /// values to the key property's actual CLR type (handles both <c>int</c> and <c>int?</c> keys).
    /// </summary>
    private static Expression<Func<T, bool>> BuildKeyPredicate<T>(
        string keyProperty, IReadOnlyCollection<long> keyValues)
    {
        var prop = typeof(T).GetProperty(
                       keyProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                   ?? throw new InvalidOperationException(
                       $"Key property '{keyProperty}' not found on entity '{typeof(T).Name}'.");

        var propType = prop.PropertyType;                                   // int or int?
        var underlying = Nullable.GetUnderlyingType(propType) ?? propType;  // int

        // A typed array of the key's CLR type, so EF translates to a parameterized IN (...) clause.
        var values = Array.CreateInstance(propType, keyValues.Count);
        var i = 0;
        foreach (var value in keyValues)
            values.SetValue(Convert.ChangeType(value, underlying), i++);

        var parameter = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(parameter, prop);
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(propType);
        var collection = Expression.Constant(values, typeof(IEnumerable<>).MakeGenericType(propType));
        var body = Expression.Call(containsMethod, collection, member);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
