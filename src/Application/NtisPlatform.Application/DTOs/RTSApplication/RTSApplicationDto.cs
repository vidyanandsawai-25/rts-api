using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSFieldValue;

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
