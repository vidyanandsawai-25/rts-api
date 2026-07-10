using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service to dynamically link/unlink document bindings to target business entities mapped in EF Core.
/// </summary>
public interface IDynamicBindingService
{
    /// <summary>
    /// Dynamically updates the <c>DocumentBindingId</c> property on a target business entity.
    /// </summary>
    Task LinkBindingToEntityAsync(string tableName, int entityId, int bindingId, int updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dynamically unlinks (sets to null) the <c>DocumentBindingId</c> property on a target business entity.
    /// </summary>
    Task UnlinkBindingFromEntityAsync(string tableName, int entityId, int bindingId, int updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the target business entity table name is registered in the EF model and supports dynamic binding.
    /// </summary>
    bool CanLinkEntity(string tableName);
}
