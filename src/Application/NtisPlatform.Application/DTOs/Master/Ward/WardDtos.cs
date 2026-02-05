using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class WardDto : CommonBaseDtos
{
    public string WardNo { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
    public string? Description { get; set; } 
    public string? DescriptionEnglish { get; set; } 
    public int? SequenceNo { get; set; }
}

public class CreateWardDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "Ward_WardNo_Required")]
    [StringLength(10, ErrorMessage = "Ward_ZoneNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ward_ZoneNo_Required")]
    [StringLength(10, ErrorMessage = "Ward_ZoneNo_MaxLen_10")]
    public string ZoneNo { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Ward_Description_MaxLen_50")]
    public string? Description { get; set; } 

    [StringLength(50, ErrorMessage = "Ward_Description_MaxLen_50")]
    public string? DescriptionEnglish { get; set; } 

    [Range(1, 999, ErrorMessage = "Ward_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}

public class UpdateWardDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "Ward_ZoneNo_Required")]
    [StringLength(10, ErrorMessage = "Ward_ZoneNo_MaxLen_10")]
    public string ZoneNo { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Ward_Description_MaxLen_50")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Ward_Description_MaxLen_50")]
    public string? DescriptionEnglish { get; set; }

    [Range(1, 999, ErrorMessage = "Ward_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}

