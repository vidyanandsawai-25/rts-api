using System.ComponentModel.DataAnnotations;


namespace NtisPlatform.Application.DTOs;

public class SubTypeOfUseDto : BaseDtos
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int TypeOfUseId { get; set; } 
    public string? SearchKey { get; set; }
    public int? SearchSequence { get; set; }
}

public class CreateSubTypeOfUseDto : CreateBaseDtos
{
    [Required(ErrorMessage = "SubTypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;


    [Required(ErrorMessage = "SubTypeOfUse_TypeOfUseID_Required")]
    public int TypeOfUseId { get; set; }

    [StringLength(20, ErrorMessage = "SearchKey_MaxLen_20")]
    public string? SearchKey { get; set; }

    public int? SearchSequence { get; set; }
}

public class UpdateSubTypeOfUseDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "SubTypeOfUse_Description_Required")]
    [StringLength(80, ErrorMessage = "Description_MaxLen_80")]
    public string Description { get; set; } = string.Empty;


    [Required(ErrorMessage = "SubTypeOfUse_TypeOfUseID_Required")]
    public int TypeOfUseId { get; set; } 

    [StringLength(20, ErrorMessage = "SearchKey_MaxLen_20")]
    public string? SearchKey { get; set; }

    public int? SearchSequence { get; set; }
}