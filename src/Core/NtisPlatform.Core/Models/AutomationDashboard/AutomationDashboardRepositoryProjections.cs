namespace NtisPlatform.Core.Models.AutomationDashboard;

public class DashboardCardBreakdownProjection
{
    public int PropertyCount { get; set; }
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
    public decimal Demand { get; set; }
}

public class WorkflowStageProjection : AutomationDashboardWorkflowStageKeyDto
{
    public string StageName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class WorkflowStageCountProjection : AutomationDashboardWorkflowStageKeyDto
{
    public int PropertyCount { get; set; }
    public int StructureCount { get; set; }
    public int UnitCount { get; set; }
}

public class WorkflowStageCompletionProjection : AutomationDashboardWorkflowStageKeyDto
{
    public string StageName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsCompleted { get; set; }
}
