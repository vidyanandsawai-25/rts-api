using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models.AutomationDashboard;

public  class SubGridQueryParameters
{
    [Range(1, int.MaxValue, ErrorMessage = "The ZoneId is Required")]
    public int? ZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The WorkflowStageId is Required")]
    public int? WorkflowStageId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The WardId is Required")]
    public int? WardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeCategoryId is Required")]
    public int? PropertyTypeCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeId is Required")]
    public int? PropertyTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The AssessmentTypeId is Required")]
    public int? AssessmentTypeId { get; set; }

    [StringLength(100, ErrorMessage = "The PropertyNo must not exceed 100 characters")]
    public string? PropertyNo { get; set; }

    [StringLength(250, ErrorMessage = "The OwnerName must not exceed 250 characters")]
    public string? OwnerName { get; set; }

    [DefaultValue(null)]
    public bool? Structure { get; set; }

    [DefaultValue(null)]
    public bool? Unit { get; set; }

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

public  class WardSubGridQueryParameters
{
    [Required(ErrorMessage = "The WardId is Required")]
    [Range(1, int.MaxValue, ErrorMessage = "The WardId is Required")]
    public int? WardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The WorkflowStageId is Required")]
    public int? WorkflowStageId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeCategoryId is Required")]
    public int? PropertyTypeCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeId is Required")]
    public int? PropertyTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The AssessmentTypeId is Required")]
    public int? AssessmentTypeId { get; set; }

    [StringLength(100, ErrorMessage = "The PropertyNo must not exceed 100 characters")]
    public string? PropertyNo { get; set; }

    [StringLength(250, ErrorMessage = "The OwnerName must not exceed 250 characters")]
    public string? OwnerName { get; set; }

    [DefaultValue(null)]
    public bool? Structure { get; set; }

    [DefaultValue(null)]
    public bool? Unit { get; set; }

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

public  class PendingAssessmentQueryParameters
{
    [StringLength(250, ErrorMessage = "The SearchTerm must not exceed 250 characters")]
    public string? SearchTerm { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The SurveyTypeId is Required")]
    public int? SurveyTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The ZoneId is Required")]
    public int? ZoneId { get; set; }

    [StringLength(50, ErrorMessage = "The ZoneNo must not exceed 50 characters")]
    public string? ZoneNo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The WardId is Required")]
    public int? WardId { get; set; }

    [StringLength(50, ErrorMessage = "The WardNo must not exceed 50 characters")]
    public string? WardNo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "The PropertyTypeId is Required")]
    public int? PropertyTypeId { get; set; }

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
