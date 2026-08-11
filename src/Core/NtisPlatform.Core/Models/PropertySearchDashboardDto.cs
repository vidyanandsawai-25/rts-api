namespace NtisPlatform.Core.Models;

public class DashboardCardBreakdownDto
{
    public int PropertyCount { get; set; }
    public int StructureCount { get; set; }
    /// <summary>
    /// Total unit count: includes all properties (structures + units).
    /// Since all properties are units, this equals PropertyCount.
    /// </summary>
    public int UnitCount { get; set; }
    public decimal Demand { get; set; }
}

public class AssessmentApprovedDto
{
    public DashboardCardBreakdownDto Assessed { get; set; } = new();
    public DashboardCardBreakdownDto Unassessed { get; set; } = new();
}

public class MainCardsResponseDto
{
    public DashboardCardBreakdownDto PreviouslyRegistered { get; set; } = new();
    public AssessmentApprovedDto AssessmentApproved { get; set; } = new();
    public DashboardCardBreakdownDto AdditionalRevenueGenerated { get; set; } = new();
}

public class WorkflowStageCardDto
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int PropertyCount { get; set; }
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}

