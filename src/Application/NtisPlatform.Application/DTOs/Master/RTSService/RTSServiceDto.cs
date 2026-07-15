using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RTSServiceMaster;

public class RTSServiceDto : BaseDtos
{
    public int DepartmentId { get; set; }

    /// <summary>
    /// Government RTS portal service reference code (e.g., 7204 = Birth Certificate).
    /// </summary>
    public int? GovtServiceCode { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceNameLocal { get; set; }
    public string? Description { get; set; }
    public string? ServiceUrl { get; set; }
    public string? ServiceIcon { get; set; }
    public int DisplayOrder { get; set; }
    public string? Sla { get; set; }
    public decimal? Fees { get; set; }
    public bool FeesRequired { get; set; }
}

public class CreateRTSServiceDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Service_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Service_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Service_GovtServiceCode_Invalid")]
    public int? GovtServiceCode { get; set; }

    [Required(ErrorMessage = "Service_ServiceName_Required")]
    [StringLength(200, ErrorMessage = "Service_ServiceName_Required")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Service_ServiceNameLocal_MaxLengthExceeded_300")]
    public string? ServiceNameLocal { get; set; }

    [StringLength(500, ErrorMessage = "Service_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Service_ServiceUrl_MaxLengthExceeded_500")]
    public string? ServiceUrl { get; set; }

    [StringLength(100, ErrorMessage = "Service_ServiceIcon_MaxLengthExceeded_100")]
    public string? ServiceIcon { get; set; }

    public int DisplayOrder { get; set; }
    public string? Sla { get; set; }
    public decimal? Fees { get; set; }
    public bool FeesRequired { get; set; }
}

public class UpdateRTSServiceDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Service_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Service_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Service_GovtServiceCode_Invalid")]
    public int? GovtServiceCode { get; set; }

    [Required(ErrorMessage = "Service_ServiceName_Required")]
    [StringLength(200, ErrorMessage = "Service_ServiceName_Required")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Service_ServiceNameLocal_MaxLengthExceeded_300")]
    public string? ServiceNameLocal { get; set; }

    [StringLength(500, ErrorMessage = "Service_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Service_ServiceUrl_MaxLengthExceeded_500")]
    public string? ServiceUrl { get; set; }

    [StringLength(100, ErrorMessage = "Service_ServiceIcon_MaxLengthExceeded_100")]
    public string? ServiceIcon { get; set; }

    public int DisplayOrder { get; set; }
    public string? Sla { get; set; }
    public decimal? Fees { get; set; }
    public bool FeesRequired { get; set; }
}