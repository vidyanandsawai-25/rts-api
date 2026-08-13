using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models.AutomationDashboard;

/// <summary>
/// Query parameters for dashboard grid endpoints (GeoSequencing, InternalSurvey, DataEntry, Assessment).
/// </summary>
public  class DashboardGridQueryParameters
{
    [Required(ErrorMessage = "The WorkflowStageId is Required")]
    [Range(1, int.MaxValue, ErrorMessage = "The WorkflowStageId is Required")]
    public int? WorkflowStageId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeId is Required")]
    public int? PropertyTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeCategoryId is Required")]
    public int? PropertyTypeCategoryId { get; set; }
}

/// <summary>
/// Query parameters for Assessment grid endpoint with additional type parameter.
/// </summary>
public  class AssessmentGridQueryParameters
{
    [Required(ErrorMessage = "The WorkflowStageId is Required")]
    [Range(1, int.MaxValue, ErrorMessage = "The WorkflowStageId is Required")]
    public int? WorkflowStageId { get; set; }

    [Required(ErrorMessage = "The Type is Required")]
    [StringLength(50, ErrorMessage = "The Type must not exceed 50 characters")]
    public string? Type { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeId is Required")]
    public int? PropertyTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeCategoryId is Required")]
    public int? PropertyTypeCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The TypeOfUseId is Required")]
    public int? TypeOfUseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The CategoryId is Required")]
    public int? CategoryId { get; set; }

    [StringLength(100, ErrorMessage = "The PropertyNo must not exceed 100 characters")]
    public string? PropertyNo { get; set; }

    [StringLength(250, ErrorMessage = "The OwnerName must not exceed 250 characters")]
    public string? OwnerName { get; set; }
}

/// <summary>
/// Query parameters for ward-wise summary endpoints (GeoSequencing, InternalSurvey, DataEntry).
/// </summary>
public  class WardWiseSummaryQueryParameters
{
    [Range(1, int.MaxValue, ErrorMessage = "The ZoneId is Required")]
    public int ZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The WorkflowStageId is Required")]
    public int WorkflowStageId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeId is Required")]
    public int? PropertyTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeCategoryId is Required")]
    public int? PropertyTypeCategoryId { get; set; }

    private int _pageNumber = 1;
    private int _pageSize = 10;
    private const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "The PageNumber is Required")]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    [Range(-1, MaxPageSize, ErrorMessage = "The PageSize must be -1 or between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value == -1 ? -1 : (value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value));
    }
}
