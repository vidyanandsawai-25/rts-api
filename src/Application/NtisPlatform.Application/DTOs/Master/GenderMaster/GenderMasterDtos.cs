using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.GenderMaster;

public class GenderMasterDtos : BaseDtos
{
    public string GenderName { get; set; } = string.Empty;
}

public class CreateGenderMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "GenderName_Required")]
    [StringLength(50, ErrorMessage = "GenderName_MaxLen_50")]
    [RegularExpression(@"^[A-Za-z\u0900-\u097F\s]+$", ErrorMessage = "GenderName_Only_Characters_Allowed")]
    public string GenderName { get; set; } = string.Empty;
}

public class UpdateGenderMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "GenderName_Required")]
    [StringLength(50, ErrorMessage = "GenderName_MaxLen_50")]
    [RegularExpression(@"^[A-Za-z\u0900-\u097F\s]+$", ErrorMessage = "GenderName_Only_Characters_Allowed")]
    public string GenderName { get; set; } = string.Empty;
}