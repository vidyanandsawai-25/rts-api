using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service responsible for validating property combination operations
/// </summary>
public interface ICombinePropertyValidator
{
    /// <summary>
    /// Validates whether properties can be combined based on ownership and existence
    /// </summary>
    /// <param name="mainPropertyId">The main property ID to combine into</param>
    /// <param name="combinePropertyIds">List of property IDs to be combined</param>
    /// <param name="overrideOwnerNameMismatch">If true, allows combining even when owner names differ</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing validation result, error message, and valid properties</returns>
    Task<(bool IsValid, string? ErrorMessage, List<PropertyEntity> ValidProperties)> ValidatePropertiesForCombinationAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        bool overrideOwnerNameMismatch = false,
        CancellationToken cancellationToken = default);
}