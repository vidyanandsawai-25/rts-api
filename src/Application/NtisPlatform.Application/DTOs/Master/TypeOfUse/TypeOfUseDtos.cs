
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseDto : CommonBaseDtos
{
    public string TypeOfUseID { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEnglish { get; set; }
    public string Type { get; set; } = string.Empty;
    public string GroupID { get; set; } = string.Empty;
    public string? SearchKey { get; set; }
    public int? Sequence { get; set; }
    public bool? IsSociety { get; set; }
}

public class CreateTypeOfUseDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "TypeOfUse_TypeOfUseID_Required")]
    [StringLength(10, ErrorMessage = "TypeOfUse_TypeOfUseID_MaxLen_10")]
    public string TypeOfUseID { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "TypeOfUse_Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "TypeOfUse_DescriptionEnglish_MaxLen_80")]
    public string? DescriptionEnglish { get; set; }

    [Required(ErrorMessage = "TypeOfUse_Type_Required")]
    [StringLength(5, ErrorMessage = "TypeOfUse_Type_MaxLen_5")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_GroupID_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUse_GroupID_MaxLen_50")]
    public string GroupID { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "TypeOfUse_SearchKey_MaxLen_20")]
    public string? SearchKey { get; set; }

    public int? Sequence { get; set; }

    public bool? IsSociety { get; set; }
}

public class UpdateTypeOfUseDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "TypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "TypeOfUse_Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "TypeOfUse_DescriptionEnglish_MaxLen_80")]
    public string? DescriptionEnglish { get; set; }

    [Required(ErrorMessage = "TypeOfUse_Type_Required")]
    [StringLength(5, ErrorMessage = "TypeOfUse_Type_MaxLen_5")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUse_GroupID_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUse_GroupID_MaxLen_50")]
    public string GroupID { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "TypeOfUse_SearchKey_MaxLen_20")]
    public string? SearchKey { get; set; }

    public int? Sequence { get; set; }

    public bool? IsSociety { get; set; }
}