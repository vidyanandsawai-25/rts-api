using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>
/// Concrete adapter that satisfies <see cref="IPropertyAggregateRepository"/> for use-cases that
/// need only the aggregate-root load (e.g. <c>PropertyMutationInvariantPolicy</c> integration tests,
/// or any future cross-tab query that does not belong to a single per-tab repository).
/// <para>
/// All per-tab repositories (e.g. <c>PropertyBasicDetailsRepository</c>) already provide an
/// equivalent implementation by inheriting <see cref="PropertyRepositoryBase"/>; this class
/// is the dedicated DI registration target for <see cref="IPropertyAggregateRepository"/> so
/// that consumers do not need to depend on any particular tab's repository port.
/// </para>
/// </summary>
public sealed class PropertyAggregateRepository : PropertyRepositoryBase
{
    public PropertyAggregateRepository(ApplicationDbContext context) : base(context) { }
}
