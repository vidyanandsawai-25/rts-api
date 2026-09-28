using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyVisitTracker;

public class PropertyVisitTrackerDto : BaseDtos
{
    public int PropertyId { get; set; }

    public int WorkflowStageId { get; set; }

    public string? WorkflowStageName { get; set; }

    public string? WorkflowStageDescription { get; set; }

    public int? ModuleId { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public DateTime? VisitDateTime { get; set; }

    public string? WardNo { get; set; }

    public string? PropertyNo { get; set; }

    public string? PartitionNo { get; set; }

    public string? DisplayPropertyNo { get; set; }
}

public class CreatePropertyVisitTrackerDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertyVisitTracker_PropertyId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertyVisitTracker_PropertyId_Range")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertyVisitTracker_WorkflowStageId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertyVisitTracker_WorkflowStageId_Range")]
    public int WorkflowStageId { get; set; }

    [Required(ErrorMessage = "PropertyVisitTracker_ModuleId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertyVisitTracker_ModuleId_Range")]
    public int ModuleId { get; set; }
}

public class CreatePropertyVisitTrackerResponseDto
{
    public bool Status { get; set; }

    public string Message { get; set; } = string.Empty;

    public int VisitId { get; set; }

    public int PropertyId { get; set; }

    public int WorkflowStageId { get; set; }

    public string? WorkflowStageName { get; set; }

    public int? ModuleId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }
}

public class PropertyVisitTrackerListDto
{
    public int VisitId { get; set; }

    public int PropertyId { get; set; }

    public string? WardNo { get; set; }

    public string? PropertyNo { get; set; }

    public string? PartitionNo { get; set; }

    public string? DisplayPropertyNo { get; set; }

    public int WorkflowStageId { get; set; }

    public string? WorkflowStageName { get; set; }

    public string? WorkflowStageDescription { get; set; }

    public int? ModuleId { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public DateTime? VisitDateTime { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Location { get; set; }

    public bool IsActive { get; set; }
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

    public List<PropertyVisitTrackerListDto> VisitList { get; set; } = new();
}

public class CreatePropertySurveyVisitDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PropertySurveyVisit_PropertyId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_PropertyId_Range")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "PropertySurveyVisit_WorkflowStageId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_WorkflowStageId_Range")]
    public int WorkflowStageId { get; set; }

    [Required(ErrorMessage = "PropertySurveyVisit_ModuleId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_ModuleId_Range")]
    public int ModuleId { get; set; }

    public bool InternalSurveyVerified { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_RemarkId_Range")]
    public int? RemarkId { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "PropertySurveyVisit_RemarkText_MaxLength")]
    public string? RemarkText { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [StringLength(
        500,
        ErrorMessage = "PropertySurveyVisit_Location_MaxLength")]
    public string? Location { get; set; }
}

public class CreatePropertySurveyVisitResponseDto
{
    public bool Status { get; set; }

    public string Message { get; set; } = string.Empty;

    public int PropertyId { get; set; }

    public int PropertyWorkflowDetailsId { get; set; }

    public int SurveyVisitId { get; set; }

    public int WorkflowStageId { get; set; }

    public int? ModuleId { get; set; }

    public bool InternalSurveyVerified { get; set; }

    public int? RemarkId { get; set; }

    public string? RemarkText { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Location { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }
}

public class VerifyPropertySurveyVisitDto : CreateBaseDtos
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_PropertyId_Range")]
    public int? PropertyId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_WingDetailId_Range")]
    public int? WingDetailId { get; set; }

    [Required(ErrorMessage = "PropertySurveyVisit_WorkflowStageId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_WorkflowStageId_Range")]
    public int WorkflowStageId { get; set; }

    [Required(ErrorMessage = "PropertySurveyVisit_ModuleId_Required")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_ModuleId_Range")]
    public int ModuleId { get; set; }

    public int? RemarkId { get; set; }

    public string? RemarkText { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Location { get; set; }
}

public class UnverifyPropertySurveyVisitDto : CreateBaseDtos
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_PropertyId_Range")]
    public int? PropertyId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "PropertySurveyVisit_WingDetailId_Range")]
    public int? WingDetailId { get; set; }

    public int? RemarkId { get; set; }

    public string? RemarkText { get; set; }
}

public class PropertyVerificationStatusItemDto
{
    public int PropertyId { get; set; }

    public string PropertyNo { get; set; } = string.Empty;

    public string PartitionNo { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public bool HasPhoto { get; set; }

    public string StatusMessage { get; set; } = string.Empty;
}

public class VerifyPropertySurveyVisitResponseDto
{
    public bool Status { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? PropertyId { get; set; }

    public int? WingDetailId { get; set; }

    public int PropertyWorkflowDetailsId { get; set; }

    public int SurveyVisitId { get; set; }

    public bool IsVerified { get; set; }

    public List<PropertyVerificationStatusItemDto> VerifiedProperties { get; set; } = new();

    public List<PropertyVerificationStatusItemDto> UnverifiedProperties { get; set; } = new();
}

public class UnverifyPropertySurveyVisitResponseDto
{
    public bool Status { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? PropertyId { get; set; }

    public int? WingDetailId { get; set; }

    public bool IsVerified { get; set; } = false;

    public List<PropertyVerificationStatusItemDto> UnverifiedProperties { get; set; } = new();
}
