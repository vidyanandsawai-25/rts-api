using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RateSectionDto : BaseDtos
{
    public int RateSectionId { get; set; }
    public string RateSectionNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
public class CreateRateSectionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RateSection_RateSectionNo_Required")]
    [StringLength(20, ErrorMessage = "RateSection_RateSectionNo_MaxLen_20")]
    public string RateSectionNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "RateSection_Description_Required")]
    [StringLength(80, ErrorMessage = "RateSection_Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

}
public class UpdateRateSectionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RateSection_RateSectionNo_Required")]
    [StringLength(20, ErrorMessage = "RateSection_RateSectionNo_MaxLen_20")]
    public string RateSectionNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "RateSection_Description_Required")]
    [StringLength(80, ErrorMessage = "RateSection_Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

}

