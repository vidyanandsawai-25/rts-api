using NtisPlatform.Application.DTOs.RTSFieldValue;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSApplication;


/// <summary>
/// Create Application After submit the application
/// </summary>
public class RTSApplicationDetailsDto
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int ServiceId { get; set; }
        public string? SessionId { get; set; }
        public int? OwnerId { get; set; }
        public string ApplicationNo { get; set; } = string.Empty;
        public string? ApplicationStatus { get; set; }
        public List<RTSFieldValueDto>? FieldValues { get; set; }
    }

    public class CreateRTSApplicationDetailsDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "RTSApplicationDetails_DepartmentId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "RTSApplicationDetails_DepartmentId_InvalidRange")]
        public int DepartmentId { get; set; }
        [Required(ErrorMessage = "RTSApplicationDetails_ServiceId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "RTSApplicationDetails_ServiceId_InvalidRange")]
        public int ServiceId { get; set; }
        public string? ApplicantName { get; set; }
        public string? ApplicantMobileNo { get; set; }
        public int ApprovalFlowId { get; set; }
        public int CurrentApprovalFlowStageId { get; set; }
        public int CurrentStageOrder { get; set; }
        public int? UserId { get; set; }

        [Required(ErrorMessage = "RTSApplicationDetails_SessionId_Required")]
        public string? SessionId { get; set; }
        public int? OwnerId { get; set; }

        [Required(ErrorMessage = "RTSApplication_ApplicationStatus_Required")]
        public string? ApplicationStatus { get; set; }
        public string? Remark { get; set; }
        public List<CreateRTSFieldValueDto>? FieldValues { get; set; }

    }


    public class UpdateRTSApplicationDetailsDto : UpdateBaseDtos
    {
        public int ServiceId { get; set; }
        public int ApprovalFlowId { get; set; }
        public int CurrentApprovalFlowStageId { get; set; }
        public int CurrentStageOrder { get; set; }
        public int? UserId { get; set; }
        public string? SessionId { get; set; }

        [Required(ErrorMessage = "RTSApplication_ApplicationStatus_Required")]
        public string? ApplicationStatus { get; set; }
        public string? Remark { get; set; }
        public List<UpdateRTSFieldValueDto>? FieldValues { get; set; }

    }
