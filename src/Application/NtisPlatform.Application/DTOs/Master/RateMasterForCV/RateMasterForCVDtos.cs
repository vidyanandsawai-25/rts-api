using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RateMasterForCVDto : BaseDtos
{
    public int RateMasterCVId { get; set; }

    public int SubZoneId { get; set; }

    public int? TypeOfUseGroupId { get; set; }

    public int? FloorGroupId { get; set; }

    public decimal RateAmount { get; set; }

    public int AssessmentYearRangeId { get; set; }

    // Navigation property names (read-only, populated from joins)
    public string? SubZoneNo { get; set; }
    public string? SubZoneName { get; set; }
    public string? TypeOfUseGroupName { get; set; }
    public string? FloorGroupName { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
}

public class CreateRateMasterForCVDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "CVRate_SubZoneId_Required")]
    [Required(ErrorMessage = "CVRate_SubZoneId_Required")]
    public int SubZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CVRate_TypeOfUseGroupId_Invalid")]
    public int? TypeOfUseGroupId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CVRate_FloorGroupId_Invalid")]
    public int? FloorGroupId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_RateAmount_Min_0")]
    [Required(ErrorMessage = "CVRate_RateAmount_Required")]
    public decimal RateAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CVRate_AssessmentYearRangeId_Required")]
    [Required(ErrorMessage = "CVRate_AssessmentYearRangeId_Required")]
    public int AssessmentYearRangeId { get; set; }
}

public class UpdateRateMasterForCVDto : UpdateBaseDtos
{    
    [Range(1, int.MaxValue, ErrorMessage = "CVRate_SubZoneId_Required")]
    [Required(ErrorMessage = "CVRate_SubZoneId_Required")]
    public int SubZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CVRate_TypeOfUseGroupId_Invalid")]
    public int? TypeOfUseGroupId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CVRate_FloorGroupId_Invalid")]
    public int? FloorGroupId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "CVRate_RateAmount_Min_0")]
    [Required(ErrorMessage = "CVRate_RateAmount_Required")]
    public decimal RateAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CVRate_AssessmentYearRangeId_Required")]
    [Required(ErrorMessage = "CVRate_AssessmentYearRangeId_Required")]
    public int AssessmentYearRangeId { get; set; }
}
