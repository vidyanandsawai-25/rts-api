using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Master.CSNDetails;

namespace NtisPlatform.Application.DTOs;

public class RateMasterForCVDto : BaseDtos
{
    public int SubZoneId { get; set; }

    public int? TypeOfUseGroupCVId { get; set; }

    public int? FloorGroupId { get; set; }

    public int AssessmentYearRangeId { get; set; }

    public decimal RateAmount { get; set; }

    public List<CSNDetailsDto> CSNDetails { get; set; } = new();
}

public class CreateRateMasterForCVDto : CreateBaseDtos
{
    [Required(ErrorMessage = "CVRate_SubZoneId_Required")]
    public int SubZoneId { get; set; }

    public int? TypeOfUseGroupCVId { get; set; }

    public int? FloorGroupId { get; set; }

    [Required(ErrorMessage = "CVRate_AssessmentYearRangeId_Required")]
    public int AssessmentYearRangeId { get; set; }

    [Required(ErrorMessage = "CVRate_RateAmount_Required")]
    [Range(typeof(decimal), "0", "99999", ErrorMessage = "RateAmount_Invalid")]
    public decimal RateAmount { get; set; }


    [Required(ErrorMessage = "CVRate_CSNDetails_Required")]
    public List<CreateCSNDetailsDto> CSNDetails { get; set; } = new();
}


public class UpdateRateMasterForCVDto : UpdateBaseDtos
{

    [Required(ErrorMessage = "CVRate_SubZoneId_Required")]
    public int SubZoneId { get; set; }

    public int? TypeOfUseGroupCVId { get; set; }

    public int? FloorGroupId { get; set; }

    [Required(ErrorMessage = "CVRate_AssessmentYearRangeId_Required")]
    public int AssessmentYearRangeId { get; set; }

    [Required(ErrorMessage = "CVRate_RateAmount_Required")]
    [Range(typeof(decimal), "0", "99999", ErrorMessage = "RateAmount_Invalid")]
    public decimal RateAmount { get; set; }

    [Required(ErrorMessage = "CVRate_CSNDetails_Required")]
    public List<UpdateCSNDetailsDto> CSNDetails { get; set; } = new();
}

