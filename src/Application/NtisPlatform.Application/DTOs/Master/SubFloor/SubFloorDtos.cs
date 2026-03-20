using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class SubFloorDto
{
    public int SubFloorId { get; set; } 
    public string? SubFloorCode { get; set; }
    public string? Description { get; set; }
    public decimal? SubFloorPercentage { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateSubFloorDto
{
    // DB keys -> translated via IStringLocalizer ("ValidationMessages" resource)
    [Required(ErrorMessage = "SubFloorId_Required")]
    [StringLength(5, ErrorMessage = "SubFloorId_MaxLen_5")]
    public string SubFloorCode { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }
    public decimal? SubFloorPercentage { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
}

public class UpdateSubFloorDto
{
    // DB keys -> translated via IStringLocalizer ("ValidationMessages" resource)
    [Required(ErrorMessage = "SubFloorId_Required")]
    [StringLength(5, ErrorMessage = "SubFloorId_MaxLen_5")]
    public string SubFloorCode { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public decimal? SubFloorPercentage { get; set; }
    public bool IsActive { get; set; }
    public int? UpdatedBy { get; set; }

}
