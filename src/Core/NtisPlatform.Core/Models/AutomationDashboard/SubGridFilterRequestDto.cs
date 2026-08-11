namespace NtisPlatform.Core.Models;

public  class SubGridFilterRequestDto
{
    public int? ZoneId { get; set; }

    public int? WorkflowStageId { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int? WardId { get; set; }

    public int? PropertyTypeCategoryId { get; set; }

    public int? PropertyTypeId { get; set; }

    public int? AssessmentTypeId { get; set; }

    public int? SurveyTypeId { get; set; }

    public string? ZoneNo { get; set; }

    public string? WardNo { get; set; }

    public string? SearchTerm { get; set; }

    public string? PropertyNo { get; set; }

    public string? OwnerName { get; set; }
}
