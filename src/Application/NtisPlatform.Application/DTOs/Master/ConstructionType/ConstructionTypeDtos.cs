using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class ConstructionTypeDto : BaseDtos
{
    public int Id { get; set; }
    public string ConstructionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SearchKey { get; set; } = string.Empty;
    public int? SearchSequence { get; set; }
}

public class CreateConstructionTypeDto : CreateBaseDtos
{
    // DB key -> will be translated via IStringLocalizer ("ValidationMessages" resource)
    [Required(ErrorMessage = "ConstructionId_Required")]
    [StringLength(7, ErrorMessage = "ConstructionId_MaxLen_7")]
    public string ConstructionCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Construction_Description_MaxLen_100")]
    public string Description { get; set; } = string.Empty;


    [StringLength(20, ErrorMessage = "Construction_KeyboardShortCutKey_MaxLen_20")]
    public string? SearchKey { get; set; } = string.Empty;

    public int? SearchSequence { get; set; }
}

public class UpdateConstructionTypeDto : UpdateBaseDtos
{
    // DB key -> will be translated via IStringLocalizer ("ValidationMessages" resource)
    [Required(ErrorMessage = "ConstructionId_Required")]
    [StringLength(7, ErrorMessage = "ConstructionId_MaxLen_7")]
    public string ConstructionCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Construction_Description_MaxLen_100")]
    public string Description { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Construction_KeyboardShortCutKey_MaxLen_20")]
    public string? SearchKey { get; set; } = string.Empty;
    public int? SearchSequence { get; set; }
}
