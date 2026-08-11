using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSFieldValue;

    /// <summary>
    /// Application Dashboard cards Counts
    /// </summary>
    public class RTSApplicationDashboardCountsDto  
    {
        public int TotalApplications { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Reverted { get; set; }
        public int TodayApplications { get; set; }
        public int? OverdueApplications { get; set; }
        public int DueToday { get; set; }

        /// <summary>
        /// Application Dashboard Cards appplication status wise percentage Percentage 
        /// </summary>
        public decimal PendingPercentage { get; set; }
        public decimal ApprovedPercentage { get; set; }
        public decimal RejectedPercentage { get; set; }
        public decimal RevertedPercentage { get; set; }
        public decimal TodayPercentage { get; set; }
        public decimal DueTodayPercentage { get; set; }
        public decimal OverduePercentage { get; set; }
    }

    /// <summary>
    /// RTS Application approval Dashboard Application details
    /// </summary>
    public class RTSApplicationDashboardDetailsDto   
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int ServiceId { get; set; }
        public string? ApplicationNo { get; set; }
        public string? ApplicationStatus { get; set; }
        public string? Remark { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int AssignedTo { get; set; }
        public int Action { get; set; }
        public string? SessionId { get; set; }
        public int? OwnerId { get; set; }
        public string? DepartmentName { get; set; }
        public string? CitizenName { get; set; }
        public string? ServiceName { get; set; }
        public string? Sla { get; set; }
        public string? RemainingDays { get; set; }
        public string? DueDays { get; set; }
        public string? OverdueDays { get; set; }
        public List<ApplicantFieldDto>? ApplicantDetails { get; set; }
    }

    /// <summary>
    /// RTS Application approval Dashboard combined responce
    /// </summary>
    public class RTSApplicationDashboardResponseDto
    {
        public RTSApplicationDashboardCountsDto Dashboard { get; set; } = new RTSApplicationDashboardCountsDto();
        public List<RTSApplicationDashboardDetailsDto> Applications { get; set; } = new List<RTSApplicationDashboardDetailsDto>();
    }

    /// <summary>
    /// get Applicant Name And Basic Details From Fieldvalue to show Dashboard
    /// </summary>
    public class ApplicantFieldDto
    {
        public string? FieldLabel { get; set; }
        public string? FieldValue { get; set; }
    }


    /// <summary>
    /// Create Application After submit the application
    /// </summary>
    public class RTSApplicationDetailsDto
    {
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

        [Required(ErrorMessage = "RTSApplicationDetails_SessionId_Required")]
        public string? SessionId { get; set; }

        public int? OwnerId { get; set; }

        [Required(ErrorMessage = "RTSApplication_ApplicationStatus_Required")]
        public string? ApplicationStatus { get; set; }
        public List<CreateRTSFieldValueDto>? FieldValues { get; set; }

    }
