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
    Task<(bool IsValid, string? ErrorMessage, List<PropertyEntity> ValidProperties)> ValidatePropertiesForCombinationAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        CancellationToken cancellationToken = default);
}