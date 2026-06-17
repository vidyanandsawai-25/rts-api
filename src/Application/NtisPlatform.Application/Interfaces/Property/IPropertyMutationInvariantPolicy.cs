using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Single enforcement boundary for all Property aggregate write invariants.
/// <para>
/// Every mutating use-case service MUST call <see cref="EnforceAsync"/> after confirming
/// the property exists (i.e. after a successful <c>GetActivePropertyAsync</c> call) and
/// before applying any state changes. This guarantees that every mutation path — across all
/// per-tab services — runs through one place, so a new aggregate rule is added here once
/// and immediately protects every write operation.
/// </para>
/// <para>
/// Throws <see cref="NtisPlatform.Application.Exceptions.PropertyValidationException"/>
/// when an invariant is violated. The exception bubbles to
/// <c>PropertyApiExceptionFilter</c> which maps it to 400 Bad Request.
/// </para>
/// </summary>
public interface IPropertyMutationInvariantPolicy
{
    /// <summary>
    /// Enforces all Property aggregate write invariants against the already-loaded aggregate root.
    /// Must be invoked on every mutation path, after confirming the property exists but before
    /// any state changes are applied.
    /// </summary>
    Task EnforceAsync(PropertyEntity property, CancellationToken cancellationToken = default);
}
