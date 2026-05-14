using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Validates property combination operations
/// </summary>
public class CombinePropertyValidator : ICombinePropertyValidator
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly ILogger<CombinePropertyValidator> _logger;

    public CombinePropertyValidator(
        IRepository<PropertyEntity, int> propertyRepository,
        ILogger<CombinePropertyValidator> logger)
    {
        _propertyRepository = propertyRepository;
        _logger = logger;
    }

    public async Task<(bool IsValid, string? ErrorMessage, List<PropertyEntity> ValidProperties)> ValidatePropertiesForCombinationAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        CancellationToken cancellationToken = default)
    {
        // Validate MainPropertyId exists
        var mainProperty = await _propertyRepository.GetByIdAsync(mainPropertyId, cancellationToken);
        if (mainProperty == null)
        {
            return (false, "MainPropertyId not found.", []);
        }

        // Validate main property is not already a combined property
        if (mainProperty.IsCombineProperty)
        {
            _logger.LogWarning("Attempted to combine properties using main property {MainPropertyId} which is already marked as combined", mainPropertyId);
            return (false, "Main property is already part of a combined property. Cannot combine already-combined properties.", []);
        }

        // Validate all CombinedPropertyIds exist
        var existingCombineProperties = await _propertyRepository.GetQueryable()
            .Where(p => combinePropertyIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(cancellationToken);

        if (existingCombineProperties.Count != combinePropertyIds.Count)
        {
            return (false, "One or more CombinedPropertyIds not found.", []);
        }

        // Validate none of the properties to be combined are already marked as combined
        var alreadyCombinedProperties = existingCombineProperties
            .Where(p => p.IsCombineProperty)
            .ToList();

        if (alreadyCombinedProperties.Count > 0)
        {
            var combinedIds = string.Join(", ", alreadyCombinedProperties.Select(p => p.Id));
            _logger.LogWarning("Attempted to combine properties that are already marked as combined: {CombinedPropertyIds}", combinedIds);
            return (false, $"One or more properties are already part of a combined property (IDs: {combinedIds}). Cannot combine already-combined properties.", []);
        }

        // Validate OwnerName is same for main property and all combined properties
        var ownerValidationResult = ValidateOwnerNames(mainProperty, existingCombineProperties);
        if (!ownerValidationResult.IsValid)
        {
            return (false, "Owner name must match for all properties.", []);
        }

        return (true, null, existingCombineProperties);
    }

    private static (bool IsValid, string? MismatchedOwnerName) ValidateOwnerNames(
        PropertyEntity mainProperty,
        List<PropertyEntity> combineProperties)
    {
        var mainOwnerName = (mainProperty.OwnerName ?? string.Empty).Trim();

        foreach (var property in combineProperties)
        {
            var combineOwnerName = (property.OwnerName ?? string.Empty).Trim();
            if (!string.Equals(mainOwnerName, combineOwnerName, StringComparison.OrdinalIgnoreCase))
            {
                return (false, combineOwnerName);
            }
        }

        return (true, null);
    }
}