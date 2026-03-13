using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class ZoneDto : BaseDtos
{
    public string ZoneNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEnglish { get; set; } 
    public int? SequenceNo { get; set; }
}

public class CreateZoneDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Zone_ZoneNo_Required")]
    [StringLength(10, ErrorMessage = "Zone_ZoneNo_MaxLen_10")]
    public string ZoneNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zone_Description_Required")]
    [StringLength(20, ErrorMessage = "Zone_Description_MaxLen_20")]
    public string Description { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Zone_Description_MaxLen_20")]
    public string? DescriptionEnglish { get; set; }

    [Range(1, 999, ErrorMessage = "Zone_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }

}


public class UpdateZoneDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Zone_Description_Required")]
    [StringLength(20, ErrorMessage = "Zone_Description_MaxLen_20")]
    public string Description { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Zone_Description_MaxLen_20")]
    public string? DescriptionEnglish { get; set; } 

    [Range(1, 999, ErrorMessage = "Zone_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}