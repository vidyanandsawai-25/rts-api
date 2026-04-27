
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseDto : BaseDtos
{
    public int Id { get; set; } 
    public string TypeOfUseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int TypeOfUseGroupId { get; set; } 
    public int? SearchSequence { get; set; }
}

public class CreateTypeOfUseDto : CreateBaseDtos
{
    [Required(ErrorMessage = "TypeOfUse_TypeOfUseID_Required")]
    [StringLength(10, ErrorMessage = "TypeOfUse_TypeOfUseID_MaxLen_10")]
    public string TypeOfUseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "TypeOfUse_Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_Type_Required")]
    [StringLength(5, ErrorMessage = "TypeOfUse_Type_MaxLen_5")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_GroupID_Required")]
    public int TypeOfUseGroupId { get; set; } 
    public int? SearchSequence { get; set; }
}

public class UpdateTypeOfUseDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TypeOfUse_TypeOfUseID_Required")]
    [StringLength(10, ErrorMessage = "TypeOfUse_TypeOfUseID_MaxLen_10")]
    public string TypeOfUseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "TypeOfUse_Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_Type_Required")]
    [StringLength(5, ErrorMessage = "TypeOfUse_Type_MaxLen_5")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_GroupID_Required")]
    public int TypeOfUseGroupId { get; set; } 
    public int? SearchSequence { get; set; }
}