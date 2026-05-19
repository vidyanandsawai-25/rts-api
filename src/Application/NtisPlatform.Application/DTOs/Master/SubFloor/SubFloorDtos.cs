using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class SubFloorDto
{
    public int Id { get; set; } 
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
    [Required(ErrorMessage = "SubFloor_SubFloorId_Required")]
    [StringLength(20, ErrorMessage = "SubFloor_SubFloorId_MaxLen_20")]
    public string SubFloorCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubFloor_Description_Required")]
    [StringLength(200, ErrorMessage = "SubFloor__Description_MaxLen_200")]
    public string Description { get; set; }
    public decimal? SubFloorPercentage { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
}

public class UpdateSubFloorDto
{
    // DB keys -> translated via IStringLocalizer ("ValidationMessages" resource)
    [Required(ErrorMessage = "SubFloor_SubFloorId_Required")]
    [StringLength(20, ErrorMessage = "SubFloor_SubFloorId_MaxLen_20")]
    public string SubFloorCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubFloor_Description_Required")]
    [StringLength(200, ErrorMessage = "SubFloor__Description_MaxLen_200")]
    public string Description { get; set; }

    public decimal? SubFloorPercentage { get; set; }
    public bool IsActive { get; set; }
    public int? UpdatedBy { get; set; }

}
