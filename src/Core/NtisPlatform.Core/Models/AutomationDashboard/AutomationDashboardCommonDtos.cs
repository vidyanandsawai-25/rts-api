namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Common property identifier used by AutomationDashboard DTO/query models.
/// </summary>
public abstract class AutomationDashboardPropertyKeyDto
{
    public int PropertyId { get; set; }
}

/// <summary>
/// Common zone identifier used by AutomationDashboard DTO/query models.
/// </summary>
public abstract class AutomationDashboardZoneKeyDto
{
    public int ZoneId { get; set; }     
}

/// <summary>
/// Common ward identifier used by AutomationDashboard DTO/query models.
/// </summary>
public abstract class AutomationDashboardWardKeyDto
{
    public int WardId { get; set; }
}

/// <summary>
/// Common ward display fields used by ward-wise grid DTOs.
/// </summary>
public abstract class AutomationDashboardWardDisplayDto : AutomationDashboardWardKeyDto
{
    public string WardNo { get; set; } = string.Empty;
}

/// <summary>
/// Common workflow-stage identifier used by AutomationDashboard DTO/query models.
/// </summary>
public abstract class AutomationDashboardWorkflowStageKeyDto
{
    public int WorkflowStageId { get; set; }
}

/// <summary>
/// Common zone display fields used by grid DTOs and query results.
/// </summary>
public abstract class AutomationDashboardZoneDisplayDto : AutomationDashboardZoneKeyDto
{
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
}

/// <summary>
/// Common property + zone fields used by stage query models.
/// </summary>
public abstract class AutomationDashboardPropertyZoneDto : AutomationDashboardPropertyKeyDto
{
    public int ZoneId { get; set; }
}

/// <summary>
/// Common property + zone + ward fields used by stage query models.
/// </summary>
public abstract class AutomationDashboardPropertyWardZoneDto : AutomationDashboardPropertyZoneDto
{
    public int WardId { get; set; }
}

/// <summary>
/// Common property + zone display fields used by assessment query models.
/// </summary>
public abstract class AutomationDashboardPropertyZoneDisplayDto : AutomationDashboardPropertyZoneDto
{
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
}

/// <summary>
/// Common property + workflow-stage + zone fields used by stage query models.
/// </summary>
public abstract class AutomationDashboardStagePropertyZoneDto : AutomationDashboardPropertyZoneDto
{
    public int WorkflowStageId { get; set; }
}

/// <summary>
/// Common property + workflow-stage + ward fields used by ward-stage query models.
/// </summary>
public abstract class AutomationDashboardStagePropertyWardDto : AutomationDashboardPropertyKeyDto
{
    public int WorkflowStageId { get; set; }
    public int WardId { get; set; }
}

/// <summary>
/// Common sub-grid response scope fields.
/// </summary>
public abstract class AutomationDashboardSubGridScopeDto
{
    public int WorkflowStageId { get; set; }
    public string WorkflowStageName { get; set; } = string.Empty;
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
    public int? WardId { get; set; }
    public string? WardNo { get; set; }
}
