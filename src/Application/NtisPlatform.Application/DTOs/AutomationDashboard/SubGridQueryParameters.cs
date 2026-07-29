using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.AutomationDashboard;

public sealed class SubGridQueryParameters 
{
    public int? ZoneId { get; set; }

    public int? WorkflowStageId { get; set; }

    public int? WardId { get; set; }

    public int? PropertyTypeCategoryId { get; set; }

    public int? PropertyTypeId { get; set; }

    public int? AssessmentTypeId { get; set; }

    private int _pageNumber = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 100;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value == -1 ? -1 : (value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value));
    }
}
