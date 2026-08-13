namespace NtisPlatform.Application.Helpers.AutomationDashboard;

/// <summary>
/// Common helper class for calculating totals across all Automation Dashboard stages.
/// Generic implementation that works with any DTO type.
/// </summary>
public static class WorkflowStageTotalsCalculator
{
    /// <summary>
    /// Calculates totals for zone/division/ward data using property extraction functions.
    /// This generic method works with any DTO structure.
    /// </summary>
 
    public static TData CalculateTotals<TData>(
        List<TData> dataItems,
        Func<TData, TData> createTotalRow)
        where TData : class
    {
        if (!dataItems.Any())
            return createTotalRow(default!);

        return createTotalRow(dataItems.First());
    }

    /// <summary>
    /// Sums structure and unit counts from a list
    /// </summary>
    public static StructureUnitCount SumStructureUnitCounts<TData>(
        List<TData> dataItems,
        Func<TData, StructureUnitCount> getCount)
    {
        return new StructureUnitCount
        {
            StatusId = dataItems.Select(d => getCount(d)?.StatusId ?? 0).FirstOrDefault(id => id > 0),
            StructureCount = dataItems.Sum(d => getCount(d)?.StructureCount ?? 0),
            UnitCount = dataItems.Sum(d => getCount(d)?.UnitCount ?? 0)
        };
    }

    /// <summary>
    /// Sums property type breakdown from a list
    /// </summary>
    public static PropertyTypeBreakdown SumPropertyTypeBreakdown<TData>(
        List<TData> dataItems,
        Func<TData, PropertyTypeBreakdown> getBreakdown)
    {
        return new PropertyTypeBreakdown
        {
            Residential = dataItems.Sum(d => getBreakdown(d)?.Residential ?? 0),
            NonResidential = dataItems.Sum(d => getBreakdown(d)?.NonResidential ?? 0),
            Mixed = dataItems.Sum(d => getBreakdown(d)?.Mixed ?? 0),
            PublicUtility = dataItems.Sum(d => getBreakdown(d)?.PublicUtility ?? 0),
            UnderConstruction = dataItems.Sum(d => getBreakdown(d)?.UnderConstruction ?? 0)
        };
    }

    /// <summary>
    /// Sums assessment status breakdown from a list
    /// </summary>
    public static AssessmentStatusBreakdown SumAssessmentStatusBreakdown<TData>(
        List<TData> dataItems,
        Func<TData, AssessmentStatusBreakdown> getBreakdown)
    {
        return new AssessmentStatusBreakdown
        {
            Assessed = SumStructureUnitCounts(dataItems, d => getBreakdown(d)?.Assessed!),
            Unassessed = SumStructureUnitCounts(dataItems, d => getBreakdown(d)?.Unassessed!),
            NewlyAssessedFound = SumStructureUnitCounts(dataItems, d => getBreakdown(d)?.NewlyAssessedFound!),
            AssessmentInProcess = SumStructureUnitCounts(dataItems, d => getBreakdown(d)?.AssessmentInProcess!)
        };
    }

    /// <summary>
    /// Sums integer values from a list
    /// </summary>
    public static int Sum<TData>(List<TData> dataItems, Func<TData, int> getValue)
        => dataItems.Sum(d => getValue(d));
}
