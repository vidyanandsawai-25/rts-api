using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Default implementation of <see cref="IPropertyMutationInvariantPolicy"/>.
/// <para>
/// <c>GetActivePropertyAsync</c> (called by every mutation service before invoking this policy)
/// already guarantees <c>IsActive &amp;&amp; !MarkedForDeletion</c>, so that check is not
/// duplicated here. This class exists as the authoritative single point where future cross-cutting
/// aggregate invariants must be placed — e.g. workflow-lock checks, combine-property guards, or
/// cross-tab consistency rules.  Add new invariants here and they automatically protect every
/// mutation path without touching individual use-case services.
/// </para>
/// </summary>
public class PropertyMutationInvariantPolicy : IPropertyMutationInvariantPolicy
{
    /// <inheritdoc />
    public Task EnforceAsync(PropertyEntity property, CancellationToken cancellationToken = default)
    {
        // No aggregate invariants beyond the active check are currently enforced.
        // Future rules belong here — never scattered across individual tab services.
        return Task.CompletedTask;
    }
}
