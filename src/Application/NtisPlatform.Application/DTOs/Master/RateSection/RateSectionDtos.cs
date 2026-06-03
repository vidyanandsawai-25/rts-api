using NtisPlatform.Core;
using NtisPlatform.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

[LocalizableEntity(typeof(RateSectionEntity))]
public class RateSectionDto : BaseDtos
{
    [IsLocalizable("RateSection")]
    public string Description { get; set; } = string.Empty;
}
public class CreateRateSectionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RateSection_Description_Required")]
    [StringLength(80, ErrorMessage = "RateSection_Description_MaxLen_80")]
    [IsLocalizable("RateSection")]
    public string Description { get; set; } = string.Empty;

}
public class UpdateRateSectionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RateSection_Description_Required")]
    [StringLength(80, ErrorMessage = "RateSection_Description_MaxLen_80")]
    [IsLocalizable("RateSection")]
    public string Description { get; set; } = string.Empty;

}

