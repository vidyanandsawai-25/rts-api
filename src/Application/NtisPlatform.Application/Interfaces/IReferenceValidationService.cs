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
}
