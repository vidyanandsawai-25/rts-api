using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class MoujaDto : BaseDtos
{
    public int Id { get; set; } = 0;

    public int Year { get; set; } = 0;

    public string MoujaName { get; set; } = string.Empty;

}

public class CreateMoujaDto : CreateBaseDtos
{
    [Range(1, 9999, ErrorMessage = "Mouja_Year_Range_1900_9999")]
    [Required(ErrorMessage = "Mouja_Year_Required")]
    public int Year { get; set; } = 0;
    [StringLength(50, ErrorMessage = "Mouja_MoujaName_MaxLen_50")]
    [Required(ErrorMessage = "Mouja_MoujaName_Required")]
    public string MoujaName { get; set; } = string.Empty;
}

public class UpdateMoujaDto : UpdateBaseDtos
{
    [Range(1, 9999, ErrorMessage = "Mouja_Year_Range_1900_9999")]
    [Required(ErrorMessage = "Mouja_Year_Required")]
    public int Year { get; set; } = 0;
    [StringLength(50, ErrorMessage = "Mouja_MoujaName_MaxLen_50")]
    [Required(ErrorMessage = "Mouja_MoujaName_Required")]
    public string MoujaName { get; set; } = string.Empty;
}
