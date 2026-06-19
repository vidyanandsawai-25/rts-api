using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Constants;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Validates property combination operations
/// </summary>
public class CombinePropertyValidator : ICombinePropertyValidator
{
    private const string InactiveOrLockedErrorMessage = "Property cannot be combined because it is inactive or locked.";

    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly IPolicyConfigurationService _policyConfigurationService;
    private readonly ILogger<CombinePropertyValidator> _logger;

    public CombinePropertyValidator(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        IPolicyConfigurationService policyConfigurationService,
        ILogger<CombinePropertyValidator> logger)
    {
        _propertyRepository = propertyRepository;
        _categoryRepository = categoryRepository;
        _policyConfigurationService = policyConfigurationService;
        _logger = logger;
    }

    public async Task<(bool IsValid, string? ErrorMessage, List<PropertyEntity> ValidProperties)> ValidatePropertiesForCombinationAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        bool overrideOwnerNameMismatch = false,
        CancellationToken cancellationToken = default)
    {
        // Validate SourcePropertyId exists
        var mainProperty = await _propertyRepository.GetByIdAsync(mainPropertyId, cancellationToken);
        if (mainProperty == null)
        {
            return (false, "SourcePropertyId not found.", []);
        }

        // Validate source property active/locked status
        if (!mainProperty.IsActive || mainProperty.MarkedForDeletion)
        {
            _logger.LogWarning("Source property {MainPropertyId} is inactive or locked", mainPropertyId);
            return (false, InactiveOrLockedErrorMessage, []);
        }

        // Validate all CombinedPropertyIds exist
        var existingCombineProperties = await _propertyRepository.GetQueryable()
            .Where(p => combinePropertyIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(cancellationToken);

        if (existingCombineProperties.Count != combinePropertyIds.Count)
        {
            return (false, "One or more CombinedPropertyIds not found.", []);
        }

        // Validate combined properties locked status (MarkedForDeletion)
        // Note: IsActive check is unnecessary here since the query already filters by IsActive
        if (existingCombineProperties.Any(p => p.MarkedForDeletion))
        {
            _logger.LogWarning("One or more combined properties are marked for deletion");
            return (false, InactiveOrLockedErrorMessage, []);
        }

        // Validate OwnerName is same for main property and all combined properties
        // Skip this validation if overrideOwnerNameMismatch is true (user confirmed to proceed)
        if (!overrideOwnerNameMismatch)
        {
            var ownerValidationResult = ValidateOwnerNames(mainProperty, existingCombineProperties);
            if (!ownerValidationResult.IsValid)
            {
                return (false, "Owner name must match for all properties.", []);
            }
        }
        // Get main property category
        var mainCategory = mainProperty.CategoryId.HasValue
            ? await _categoryRepository.GetByIdAsync(mainProperty.CategoryId.Value, cancellationToken)
            : null;
        bool isApartment = mainCategory != null &&
            mainCategory.PropertyCategoryName != null &&
            mainCategory.PropertyCategoryName.IndexOf(CapitalValueConstants.PropertyCategory.ApartmentKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (isApartment)
        {
            var apartmentCategoryValidation = await ValidateApartmentCategoryAsync(mainProperty, mainCategory!, existingCombineProperties, cancellationToken);
            if (!apartmentCategoryValidation.IsValid)
            {
                return (false, apartmentCategoryValidation.ErrorMessage, []);
            }
        }
        else
        {
            var nonApartmentCategoryValidation = await ValidateNonApartmentCategoryAsync(mainProperty, existingCombineProperties, cancellationToken);
            if (!nonApartmentCategoryValidation.IsValid)
            {
                return (false, nonApartmentCategoryValidation.ErrorMessage, []);
            }
        }
        // Validate that properties are not occupier-only (must have owner name)
        var occupierOnlyValidation = ValidateNotOccupierOnly(mainProperty, existingCombineProperties);
        if (!occupierOnlyValidation.IsValid)
        {
            return (false, occupierOnlyValidation.ErrorMessage, []);
        }

        // Validate that no properties have restricted owner names (e.g., "Holder", "धारक")
        var restrictedOwnerValidation = ValidateNoRestrictedOwnerNames(mainProperty, existingCombineProperties);
        if (!restrictedOwnerValidation.IsValid)
        {
            return (false, restrictedOwnerValidation.ErrorMessage, []);
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

    /// <summary>
    /// Validates that no properties have restricted owner names (e.g., "Holder", "The Holder", "धारक").
    /// Properties with restricted owner names cannot be combined.
    /// </summary>
    private static (bool IsValid, string? ErrorMessage) ValidateNoRestrictedOwnerNames(
        PropertyEntity mainProperty,
        List<PropertyEntity> combineProperties)
    {
        var mainOwnerName = (mainProperty.OwnerName ?? string.Empty).Trim();

        // Check if main property has restricted owner name
        if (IsRestrictedOwnerName(mainOwnerName))
        {
            return (false, $"Main property has restricted owner name '{mainOwnerName}' and cannot be combined.");
        }

        // Check all combined properties for restricted owner names
        foreach (var property in combineProperties)
        {
            var combineOwnerName = (property.OwnerName ?? string.Empty).Trim();

            if (IsRestrictedOwnerName(combineOwnerName))
            {
                return (false, $"Property (ID: {property.Id}) has restricted owner name '{combineOwnerName}' and cannot be combined.");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Checks if the owner name is a restricted placeholder name that cannot be combined
    /// </summary>
    private static bool IsRestrictedOwnerName(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            return false;
        }

        return CapitalValueConstants.RestrictedOwnerNames.All
            .Any(restricted => string.Equals(ownerName, restricted, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates that properties are not occupier-only (have occupier name but no owner name).
    /// Properties with only occupier name and no owner name cannot be combined.
    /// </summary>
    private static (bool IsValid, string? ErrorMessage) ValidateNotOccupierOnly(
        PropertyEntity mainProperty,
        List<PropertyEntity> combineProperties)
    {
        // Check main property
        if (IsOccupierOnlyProperty(mainProperty))
        {
            return (false, $"Property cannot be combined: Main property (ID: {mainProperty.Id}) has only occupier name but no owner name.");
        }

        // Check all combined properties
        foreach (var property in combineProperties)
        {
            if (IsOccupierOnlyProperty(property))
            {
                return (false, $"Property cannot be combined: Property (ID: {property.Id}) has only occupier name but no owner name.");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Checks if a property has only occupier name without owner name
    /// </summary>
    private static bool IsOccupierOnlyProperty(PropertyEntity property)
    {
        var ownerName = (property.OwnerName ?? string.Empty).Trim();
        var occupierName = (property.OccupierName ?? string.Empty).Trim();

        // Property is occupier-only if it has occupier name but no owner name
        return string.IsNullOrEmpty(ownerName) && !string.IsNullOrEmpty(occupierName);
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateApartmentCategoryAsync(
        PropertyEntity mainProperty,
        PropertyCategoryEntity mainCategory,
        List<PropertyEntity> combineProperties,
        CancellationToken cancellationToken)
    {
        // mainCategory is already validated as apartment type by the caller
        var combineCategoryIds = combineProperties
            .Where(p => p.CategoryId.HasValue)
            .Select(p => p.CategoryId.Value)
            .Distinct()
            .ToList();
        var combineCategories = await _categoryRepository.GetQueryable()
            .Where(c => combineCategoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        // Determine if this is a TRUE multi-unit apartment (multiple properties with same PropertyNo)
        // A standalone apartment may have a PartitionNo (flat number) but only exists as a single property
        // A multi-unit apartment has multiple properties (wings) with the same PropertyNo
        //
        // More reliable approach: For each distinct (CategoryId, WardId, PropertyNo) across source+combined properties,
        // treat it as multi-unit if there is more than one active row in that group and at least one row has a non-empty PartitionNo
        bool hasPartitions = false;

        // Collect all properties being combined (source + combined)
        var allPropertyIds = new List<int> { mainProperty.Id };
        allPropertyIds.AddRange(combineProperties.Select(p => p.Id));

        // Get distinct property groups (CategoryId, WardId, PropertyNo) from source and combined properties
        var allProperties = new List<PropertyEntity> { mainProperty };
        allProperties.AddRange(combineProperties);

        var propertyGroups = allProperties
            .Where(p => p.CategoryId.HasValue && !string.IsNullOrWhiteSpace(p.PropertyNo))
            .Select(p => new { p.CategoryId, p.WardId, p.PropertyNo })
            .Distinct()
            .ToList();

        // For each group, check if it's a multi-unit apartment
        foreach (var group in propertyGroups)
        {
            // Check if there are multiple active properties in this group with at least one having a partition
            var groupProperties = await _propertyRepository.GetQueryable()
                .Where(x => x.CategoryId == group.CategoryId &&
                            x.WardId == group.WardId &&
                            x.PropertyNo == group.PropertyNo &&
                            x.IsActive == true &&
                            !x.MarkedForDeletion)
                .Select(x => new { x.Id, x.PartitionNo })
                .ToListAsync(cancellationToken);

            // Multi-unit if: more than one property in group AND at least one has a non-empty PartitionNo
            if (groupProperties.Count > 1 && groupProperties.Any(p => !string.IsNullOrWhiteSpace(p.PartitionNo)))
            {
                hasPartitions = true;
                break;
            }
        }

        if (hasPartitions)
        {
            // Multi-unit apartment validation (has wings)
            // Validate source property has SocietyDetailId for wing validation
            if (!mainProperty.SocietyDetailId.HasValue)
            {
                return (false, "Source property's society details not found.");
            }

            foreach (var property in combineProperties)
            {
                var category = property.CategoryId.HasValue
                    ? combineCategories.FirstOrDefault(c => c.Id == property.CategoryId.Value)
                    : null;
                if (category == null || category.PropertyCategoryName == null ||
                    category.PropertyCategoryName.IndexOf(CapitalValueConstants.PropertyCategory.ApartmentKeyword, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return (false, "All properties must be of Apartment category to combine.");
                }

                // Wing validation: Properties with same SocietyDetailId are from the same wing
                // This is the ONLY requirement for apartment combining - partition format doesn't matter
                if (property.SocietyDetailId != mainProperty.SocietyDetailId)
                {
                    return (false, "All properties must be from the same Wing.");
                }

                // Zone, Ward, PropertyNo check
                if (property.TaxZoneId != mainProperty.TaxZoneId ||
                    property.WardId != mainProperty.WardId ||
                    property.PropertyNo != mainProperty.PropertyNo)
                {
                    return (false, "All properties must be from the same Zone, Ward, and PropertyNo.");
                }
            }
        }
        else
        {
            // Standalone apartment validation (no wings/partitions)
            // No SocietyDetailId or PropertyNo validation required - only Zone and Ward
            foreach (var property in combineProperties)
            {
                var category = property.CategoryId.HasValue
                    ? combineCategories.FirstOrDefault(c => c.Id == property.CategoryId.Value)
                    : null;
                if (category == null || category.PropertyCategoryName == null ||
                    category.PropertyCategoryName.IndexOf(CapitalValueConstants.PropertyCategory.ApartmentKeyword, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return (false, "All properties must be of Apartment category to combine.");
                }

                // Zone and Ward must match for standalone apartments
                if (property.TaxZoneId != mainProperty.TaxZoneId ||
                    property.WardId != mainProperty.WardId)
                {
                    return (false, "All properties must be from the same Zone and Ward.");
                }
            }
        }

        return (true, null);
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateNonApartmentCategoryAsync(
        PropertyEntity mainProperty,
        List<PropertyEntity> combineProperties,
        CancellationToken cancellationToken)
    {
        // Non-apartment validation: caller already determined this is not an apartment property
        var combineCategoryIds = combineProperties
            .Where(p => p.CategoryId.HasValue)
            .Select(p => p.CategoryId.Value)
            .Distinct()
            .ToList();
        var combineCategories = await _categoryRepository.GetQueryable()
            .Where(c => combineCategoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        // Check if all properties have the same PropertyNo
        var allHaveSamePropertyNo = combineProperties.All(p => p.PropertyNo == mainProperty.PropertyNo);

        if (allHaveSamePropertyNo)
        {
            // Case 1: Same PropertyNo - allow combining with ANY partitions
            // No partition format validation - can be 1,2,3 or A1,A2,A3 or A1,B2,C3 - anything is allowed
        }
        else
        {
            // Fetch policy limit, default to 2
            var allowedRangeStr = await _policyConfigurationService.GetPolicyValueAsync("CombinePropertyLimit", "2", cancellationToken);
            if (!int.TryParse(allowedRangeStr, out int allowedRange))
            {
                _logger.LogWarning("Invalid policy value for CombinePropertyLimit: '{PolicyValue}'. Defaulting to 2.", allowedRangeStr);
                allowedRange = 2;
            }

            // Case 2: Different PropertyNo - apply dynamic range validation
            // PropertyNo nearest check: must be within ±allowedRange of main property number
            foreach (var property in combineProperties)
            {
                var propertyNoValidation = ValidatePropertyNoWithinRange(mainProperty.PropertyNo, property.PropertyNo, allowedRange);
                if (!propertyNoValidation.IsValid)
                {
                    return (false, propertyNoValidation.ErrorMessage);
                }
            }
        }

        // Validate category for all combined properties
        foreach (var property in combineProperties)
        {
            var category = property.CategoryId.HasValue
                ? combineCategories.FirstOrDefault(c => c.Id == property.CategoryId.Value)
                : null;
            if (category == null || (category.PropertyCategoryName != null &&
                category.PropertyCategoryName.IndexOf(CapitalValueConstants.PropertyCategory.ApartmentKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return (false, "All properties must be of Non-Apartment category to combine.");
            }

            // Zone and Ward must match
            if (property.TaxZoneId != mainProperty.TaxZoneId ||
                property.WardId != mainProperty.WardId)
            {
                return (false, "All properties must be from the same Zone and Ward.");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Validates that the property number is within the specified range of the main property number.
    /// For non-apartment properties, only nearest properties (within ±range) can be combined.
    /// If either property number is non-numeric, this validation is skipped as the proximity check
    /// only applies to numeric property numbers.
    /// </summary>
    /// <param name="mainPropertyNo">The main property number</param>
    /// <param name="combinePropertyNo">The property number to validate</param>
    /// <param name="allowedRange">The allowed range (e.g., 2 means ±2)</param>
    /// <returns>Validation result with error message if invalid</returns>
    private static (bool IsValid, string? ErrorMessage) ValidatePropertyNoWithinRange(
        string? mainPropertyNo,
        string? combinePropertyNo,
        int allowedRange)
    {
        // If main property number is not numeric, skip this validation
        // Non-numeric property numbers (e.g., "123A", "12-B") don't support proximity checks
        if (!int.TryParse(mainPropertyNo, out int mainPropNo))
        {
            return (true, null); // Skip validation - proximity check not applicable
        }

        // If combine property number is not numeric, skip this validation
        // Non-numeric property numbers don't support proximity checks
        if (!int.TryParse(combinePropertyNo, out int propNo))
        {
            return (true, null); // Skip validation - proximity check not applicable
        }

        // Check if property number is within allowed range
        if (Math.Abs(propNo - mainPropNo) > allowedRange)
        {
            return (false, $"PropertyNo {combinePropertyNo} is not within {allowedRange} of main property number {mainPropertyNo}. Only nearest properties can be combined.");
        }

        return (true, null);
    }
}