using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class MoujaDto : BaseDtos
{
    public int Id { get; set; } = 0;

    public string MoujaNo { get; set; } = string.Empty;

    public string MoujaName { get; set; } = string.Empty;

}

public class CreateMoujaDto : CreateBaseDtos
{
    [StringLength(20, ErrorMessage = "Mouja_MoujaNo_MaxLen_20")]
    [Required(ErrorMessage = "Mouja_MoujaNo_Required")]
    public string MoujaNo { get; set; } = string.Empty;
    [StringLength(100, ErrorMessage = "Mouja_MoujaName_MaxLen_100")]
    [Required(ErrorMessage = "Mouja_MoujaName_Required")]
    public string MoujaName { get; set; } = string.Empty;
}

public class UpdateMoujaDto : UpdateBaseDtos
{
    [StringLength(20, ErrorMessage = "Mouja_MoujaNo_MaxLen_20")]
    [Required(ErrorMessage = "Mouja_MoujaNo_Required")]
    public string MoujaNo { get; set; } = string.Empty;
    [StringLength(100, ErrorMessage = "Mouja_MoujaName_MaxLen_100")]
    [Required(ErrorMessage = "Mouja_MoujaName_Required")]
    public string MoujaName { get; set; } = string.Empty;
}
