using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Centralized service for validating entity references before deactivation or deletion.
/// Provides generic, configurable logic for checking if an entity is referenced in other tables.
/// </summary>
public interface IReferenceValidationService
{
    /// <summary>
    /// Validates if an entity can be deactivated or deleted based on configured references.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to validate</typeparam>
    /// <param name="entityId">The entity ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ValidationResult indicating success or failure with detailed error message listing all referencing tables</returns>
    Task<ValidationResult> ValidateReferencesAsync<TEntity>(int entityId, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    /// <summary>
    /// Retrieves the names of all tables that reference the specified entity and contain data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to check.</typeparam>
    /// <typeparam name="TKey">The type of the referenced entity's key.</typeparam>
    /// <param name="referencedId">The ID of the referenced entity.</param>
    /// <param name="referencedColumnName">The name of the referenced column (default is "Id").</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of table names that reference the specified entity and contain data.</returns>
    Task<List<string>> GetReferencingTablesWithDataAsync<TEntity, TKey>(
    TKey referencedId,
    string referencedColumnName = "Id",
    CancellationToken cancellationToken = default)
    where TEntity : class;
}
