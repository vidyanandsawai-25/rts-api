using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RTSServiceMaster;

public class RTSServiceDto : BaseDtos
{
    public int DepartmentId { get; set; }
    public int? RTSServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ServiceUrl { get; set; }
    public string? ServiceIcon { get; set; }
}

public class CreateRTSServiceDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Service_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Service_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Service_RTSServiceId_Invalid")]
    public int? RTSServiceId { get; set; }

    [Required(ErrorMessage = "Service_ServiceName_Required")]
    [StringLength(100, ErrorMessage = "Service_ServiceName_MaxLengthExceeded_300")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Service_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Service_ServiceUrl_MaxLengthExceeded_500")]
    public string? ServiceUrl { get; set; }

    [StringLength(100, ErrorMessage = "Service_ServiceIcon_MaxLengthExceeded_100")]
    public string? ServiceIcon { get; set; }
}

public class UpdateRTSServiceDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Service_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Service_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Service_RTSServiceId_Invalid")]
    public int? RTSServiceId { get; set; }

    [Required(ErrorMessage = "Service_ServiceName_Required")]
    [StringLength(300, ErrorMessage = "Service_ServiceName_MaxLengthExceeded_300")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Service_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Service_ServiceUrl_MaxLengthExceeded_500")]
    public string? ServiceUrl { get; set; }

    [StringLength(100, ErrorMessage = "Service_ServiceIcon_MaxLengthExceeded_100")]
    public string? ServiceIcon { get; set; }
}