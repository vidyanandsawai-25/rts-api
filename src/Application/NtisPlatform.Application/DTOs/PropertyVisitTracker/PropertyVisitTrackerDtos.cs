using System;
using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.PropertyVisitTracker;

public class CreatePropertyVisitTrackerDto : CreateBaseDtos
{
    public int PropertyId { get; set; }
    public int WorkflowStageId { get; set; }
    public int? ModuleId { get; set; }
}

public class CreatePropertyVisitTrackerResponseDto : CreateBaseDtos
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int VisitId { get; set; }
    public int PropertyId { get; set; }
    public int WorkflowStageId { get; set; }
    public string WorkflowStageName { get; set; } = string.Empty;
    public int? ModuleId { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class PropertyVisitTrackerListDto : BaseDtos
{
    public int VisitId { get; set; }
    public int PropertyId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? DisplayPropertyNo { get; set; }
    public int WorkflowStageId { get; set; }
    public string WorkflowStageName { get; set; } = string.Empty;
    public string? WorkflowStageDescription { get; set; }
    public int? ModuleId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime? VisitDateTime { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }
}

public class PropertyVisitTrackerResponseDto
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
    public List<PropertyVisitTrackerListDto> VisitList { get; set; } = [];
}

public class CreatePropertySurveyVisitDto : CreatePropertyVisitTrackerDto
{
    public bool? InternalSurveyVerified { get; set; }
    public int? RemarkId { get; set; }
    public string? RemarkText { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }
}

public class CreatePropertySurveyVisitResponseDto : CreateBaseDtos
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public int PropertyWorkflowDetailsId { get; set; }
    public int SurveyVisitId { get; set; }
    public int WorkflowStageId { get; set; }
    public int? ModuleId { get; set; }
    public bool? InternalSurveyVerified { get; set; }
    public int? RemarkId { get; set; }
    public string? RemarkText { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }
}

public class VerifyPropertySurveyVisitDto : CreatePropertyVisitTrackerDto
{
    public int? RemarkId { get; set; }
    public string? RemarkText { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }
}

public class VerifyPropertySurveyVisitResponseDto
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public int PropertyWorkflowDetailsId { get; set; }
    public int SurveyVisitId { get; set; }
    public bool IsVerified { get; set; }
}

public class UnverifyPropertySurveyVisitDto
{
    public int PropertyId { get; set; }
    public int? RemarkId { get; set; }
    public string? RemarkText { get; set; }
}
