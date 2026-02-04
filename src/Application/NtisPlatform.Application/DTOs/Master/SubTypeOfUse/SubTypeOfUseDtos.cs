using System.ComponentModel.DataAnnotations;


namespace NtisPlatform.Application.DTOs;

public class SubTypeOfUseDto : CommonBaseDtos
{
    public int SubTypeOfUseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEnglish { get; set; }
    public string TypeOfUseID { get; set; } = string.Empty;
    public string? SearchKey { get; set; }
    public int? SearchSequence { get; set; }
}

public class CreateSubTypeOfUseDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "SubTypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "DescriptionEnglish_MaxLen_80")]
    public string? DescriptionEnglish { get; set; }

    [Required(ErrorMessage = "SubTypeOfUse_TypeOfUseID_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUseID_MaxLen_50")]
    public string TypeOfUseID { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "SearchKey_MaxLen_20")]
    public string? SearchKey { get; set; }

    public int? SearchSequence { get; set; }
}

public class UpdateSubTypeOfUseDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "SubTypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "DescriptionEnglish_MaxLen_80")]
    public string? DescriptionEnglish { get; set; }

    [Required(ErrorMessage = "SubTypeOfUse_TypeOfUseID_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUseID_MaxLen_50")]
    public string TypeOfUseID { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "SearchKey_MaxLen_20")]
    public string? SearchKey { get; set; }

    public int? SearchSequence { get; set; }
}