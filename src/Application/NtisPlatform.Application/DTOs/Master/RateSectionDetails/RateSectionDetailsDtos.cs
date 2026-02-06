using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RateSectionDetailsDto : CommonBaseDtos
{
    public int RateSectionDetailsID { get; set; } 
    public string RateSectionNo { get; set; } = string.Empty;
    public string WardNo { get; set; } = string.Empty;
}

public class CreateRateSectionDetailsDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "RateSectionDetails_RateSectionNo_Required")]
    [StringLength(20, ErrorMessage = "RateSectionDetails_RateSectionNo_MaxLen_20")]
    public string RateSectionNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "RateSectionDetails_WardNo_Required")]
    [StringLength(10, ErrorMessage = "RateSectionDetails_WardNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;
}


public class UpdateRateSectionDetailsDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "RateSectionDetails_RateSectionNo_Required")]
    [StringLength(20, ErrorMessage = "RateSectionDetails_RateSectionNo_MaxLen_20")]
    public string RateSectionNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "RateSectionDetails_WardNo_Required")]
    [StringLength(10, ErrorMessage = "RateSectionDetails_WardNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;
}


