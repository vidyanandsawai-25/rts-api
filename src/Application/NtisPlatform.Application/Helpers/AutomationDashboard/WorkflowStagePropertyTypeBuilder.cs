namespace NtisPlatform.Application.Helpers.AutomationDashboard;

/// <summary>
/// Common helper class for building property type breakdowns across all Automation Dashboard stages.
/// Works for GeoSequencing, InternalSurvey, DataEntry, Assessment, etc.
/// </summary>
public static class WorkflowStagePropertyTypeBuilder
{
    private static readonly HashSet<string> MixedPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "R-C", "C-R", "C-I", "I-C", "I-R", "R-I"
    };

    /// <summary>
    /// Builds property type breakdown from properties and their use groups.
    /// Generic method that works with any property projection containing PropertyId and PropertyTypeCode.
    /// </summary>
    /// <typeparam name="TProperty">Property projection type (must have PropertyId and PropertyTypeCode)</typeparam>
    public static PropertyTypeBreakdown Build<TProperty>(
        List<TProperty> properties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        Func<TProperty, int> getPropertyId,
        Func<TProperty, string?> getPropertyTypeCode)
    {
        var breakdown = new PropertyTypeBreakdown();
        if (!properties.Any())
            return breakdown;

        var nonMixedProperties = new List<TProperty>();

        foreach (var property in properties)
        {
            var typeCode = getPropertyTypeCode(property);
            if (IsMixedProperty(typeCode))
                breakdown.Mixed++;
            else
                nonMixedProperties.Add(property);
        }

        var propertiesWithDetails = 0;
        foreach (var property in nonMixedProperties)
        {
            var propertyId = getPropertyId(property);
            if (!propertyUseGroups.TryGetValue(propertyId, out var useGroup))
                continue;

            propertiesWithDetails++;

            if (useGroup.Codes.Any(code => code.Equals("UC", StringComparison.OrdinalIgnoreCase)))
                breakdown.UnderConstruction++;
            else if (useGroup.Types.Any(type => type.Equals("N", StringComparison.OrdinalIgnoreCase) || 
                                               type.Equals("I", StringComparison.OrdinalIgnoreCase)))
                breakdown.PublicUtility++;
            else if (useGroup.Types.Any(type => type.Equals("R", StringComparison.OrdinalIgnoreCase)))
                breakdown.Residential++;
            else if (useGroup.Types.Any(type => type.Equals("C", StringComparison.OrdinalIgnoreCase)))
                breakdown.NonResidential++;
        }

        breakdown.Residential += nonMixedProperties.Count - propertiesWithDetails;
        return breakdown;
    }

    /// <summary>
    /// Groups property uses by property ID for efficient lookup.
    /// Generic method that works with any property use projection.
    /// </summary>
    public static Dictionary<int, PropertyUseGroup> BuildPropertyUseGroups<TPropertyUse>(
        List<TPropertyUse> propertyUses,
        Func<TPropertyUse, int> getPropertyId,
        Func<TPropertyUse, string?> getType,
        Func<TPropertyUse, string?> getCode)
        => propertyUses
            .GroupBy(x => getPropertyId(x))
            .ToDictionary(
                g => g.Key,
                g => new PropertyUseGroup(
                    g.Where(x => getType(x) != null).Select(x => getType(x)!).Distinct().ToList(),
                    g.Where(x => getCode(x) != null).Select(x => getCode(x)!).Distinct().ToList()));

    public static bool IsMixedProperty(string? propertyTypeCode)
        => !string.IsNullOrWhiteSpace(propertyTypeCode) && MixedPropertyTypes.Contains(propertyTypeCode);
}

