using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;


public interface IReferenceValidatorBuilder
{
    List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)> Build();
}

public class ReferenceValidatorBuilder<TEntity> : IReferenceValidatorBuilder where TEntity : BaseEntity
{
    private readonly List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)> _checks = new();

    public ReferenceValidatorBuilder<TEntity> CheckReferences(params (string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)[] checks)
    {
        _checks.AddRange(checks);
        return this;
    }

    List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)> IReferenceValidatorBuilder.Build() => _checks;

    // For generic usage in the configuration
    public List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)> Build() => _checks;
}

public class ReferenceValidationConfiguration
{
    private readonly Dictionary<Type, IReferenceValidatorBuilder> _builders = new();

    public ReferenceValidatorBuilder<TEntity> ForEntity<TEntity>() where TEntity : BaseEntity
    {
        var builder = new ReferenceValidatorBuilder<TEntity>();
        _builders[typeof(TEntity)] = builder;
        return builder;
    }

    public Dictionary<Type, List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)>> Build()
    {
        var result = new Dictionary<Type, List<(string, Func<ApplicationDbContext, int, IQueryable<object>>)>>();
        foreach (var kvp in _builders)
        {
            var list = kvp.Value.Build();
            result[kvp.Key] = list;
        }
        return result;
    }
}
