namespace NtisPlatform.Application.Helpers.AutomationDashboard;

/// <summary>
/// Common helper class for building assessment status breakdowns across all Automation Dashboard stages.
/// Works for GeoSequencing, InternalSurvey, DataEntry, Assessment, etc.
/// </summary>
public static class WorkflowStageAssessmentStatusBuilder
{
    private const string ApartmentCategoryName = "Apartment";

    /// <summary>
    /// Builds structure and unit counts for a specific assessment status.
    /// Generic method that works with any property projection.
    /// </summary>
    /// <typeparam name="TProperty">Property projection type</typeparam>
    public static StructureUnitCount GetStatusCounts<TProperty>(
        List<TProperty> properties,
        int statusId,
        Func<TProperty, int?> getAssessmentStatusId,
        Func<TProperty, string?> getPartitionNo,
        Func<TProperty, string?> getCategoryName = null!)
    {
        var statusProperties = properties
            .Where(p => getAssessmentStatusId(p) == statusId)
            .ToList();

        if (!statusProperties.Any())
            return new StructureUnitCount { StatusId = statusId };

        // If category name is provided, use it to detect apartment units
        var unitCount = getCategoryName != null
            ? statusProperties.Count(p => IsApartmentUnit(getPartitionNo(p), getCategoryName(p)))
            : statusProperties.Count(p => !string.IsNullOrWhiteSpace(getPartitionNo(p)));

        return new StructureUnitCount
        {
            StatusId = statusId,
            StructureCount = statusProperties.Count - unitCount,
            UnitCount = unitCount
        };
    }

    /// <summary>
    /// Builds assessment status breakdown with all status types.
    /// </summary>
    public static AssessmentStatusBreakdown BuildBreakdown<TProperty>(
        List<TProperty> properties,
        Dictionary<string, int> statusIdsByName,
        Func<TProperty, int?> getAssessmentStatusId,
        Func<TProperty, string?> getPartitionNo,
        Func<TProperty, string?> getCategoryName = null!)
    {
        return new AssessmentStatusBreakdown
        {
            Assessed = GetStatusCounts(
                properties, 
                ResolveStatusId(statusIdsByName, "ASSESSED"), 
                getAssessmentStatusId, 
                getPartitionNo, 
                getCategoryName),
            Unassessed = GetStatusCounts(
                properties, 
                ResolveStatusId(statusIdsByName, "UNASSESSED", "UN ASSESSED"), 
                getAssessmentStatusId, 
                getPartitionNo, 
                getCategoryName),
            NewlyAssessedFound = GetStatusCounts(
                properties, 
                ResolveStatusId(statusIdsByName, "PARTIALLY_ASSESSED", "PARTIALLY ASSESSED", "NEWLY_ASSESSED_FOUND", "NEWLY ASSESSED FOUND"), 
                getAssessmentStatusId, 
                getPartitionNo, 
                getCategoryName),
            AssessmentInProcess = GetStatusCounts(
                properties, 
                ResolveStatusId(statusIdsByName, "UNDER_UNASSESSED", "UNDER UNASSESSED", "ASSESSMENT_IN_PROCESS", "ASSESSMENT IN PROCESS"), 
                getAssessmentStatusId, 
                getPartitionNo, 
                getCategoryName)
        };
    }

    private static int ResolveStatusId(Dictionary<string, int> statusIdsByName, params string[] aliases)
    {
        var normalizedLookup = statusIdsByName
            .GroupBy(kvp => NormalizeStatusName(kvp.Key))
            .ToDictionary(g => g.Key, g => g.First().Value);

        foreach (var alias in aliases)
        {
            if (normalizedLookup.TryGetValue(NormalizeStatusName(alias), out var statusId))
                return statusId;
        }

        return 0;
    }

    private static string NormalizeStatusName(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    /// <summary>
    /// Counts structures (properties without partition number)
    /// </summary>
    public static int CountStructures<TProperty>(
        List<TProperty> properties,
        Func<TProperty, string?> getPartitionNo)
        => properties.Count(p => string.IsNullOrWhiteSpace(getPartitionNo(p)));

    /// <summary>
    /// Determines if a property is an apartment unit
    /// </summary>
    private static bool IsApartmentUnit(string? partitionNo, string? categoryName)
        => categoryName == ApartmentCategoryName && !string.IsNullOrWhiteSpace(partitionNo);
}


