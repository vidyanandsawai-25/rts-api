namespace NtisPlatform.Core.Exceptions;

/// <summary>
/// Exception thrown when a workflow stage is not found
/// </summary>
public class WorkflowStageNotFoundException : EntityNotFoundException
{
    public WorkflowStageNotFoundException(int workflowStageId)
        : base("WorkflowStage", workflowStageId, "WORKFLOW_STAGE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a zone is not found
/// </summary>
public class ZoneNotFoundException : EntityNotFoundException
{
    public ZoneNotFoundException(int zoneId)
        : base("Zone", zoneId, "ZONE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when invalid automation dashboard parameters are provided
/// </summary>
public class InvalidDashboardParametersException : ValidationException
{
    public InvalidDashboardParametersException(string message)
        : base(message, "INVALID_DASHBOARD_PARAMETERS")
    {
    }
}
