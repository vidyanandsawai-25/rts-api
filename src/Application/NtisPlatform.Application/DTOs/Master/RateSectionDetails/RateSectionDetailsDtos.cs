using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class RateSectionDetailsDto : BaseDtos
{
    public int RateSectionDetailsId { get; set; } 
    public int RateSectionId { get; set; } 
    public int WardId { get; set; }
    public string? WardNo { get; set; }
}

public class CreateRateSectionDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RateSectionDetails_RateSectionNo_Required")]
    [Range(1, 9999, ErrorMessage = "RateSectionDetails_RateSectionId_1_9999")]
    public int RateSectionId { get; set; } 

    [Required(ErrorMessage = "RateSectionDetails_WardNo_Required")]
    [Range(1, 9999, ErrorMessage = "RateSectionDetails_WardId_1_9999")]
    public int WardId { get; set; } 
}


public class UpdateRateSectionDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RateSectionDetails_RateSectionNo_Required")]
    [Range(1, 9999, ErrorMessage = "RateSectionDetails_RateSectionId_1_9999")]
    public int RateSectionId { get; set; } 

    [Required(ErrorMessage = "RateSectionDetails_WardId_Required")]
    public int WardId { get; set; } 
}


