using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class WardDto : BaseDtos
{
    public int Id { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public int ZoneId { get; set; } 
    public string? Description { get; set; } 
    public int? SequenceNo { get; set; }
}

public class CreateWardDto : CreateBaseDtos
{
    [StringLength(10, ErrorMessage = "Ward_ZoneNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ward_ZoneNo_Required")]
    public int ZoneId { get; set; } 

    [StringLength(50, ErrorMessage = "Ward_Description_MaxLen_50")]
    public string? Description { get; set; } 

    [Range(1, 999, ErrorMessage = "Ward_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}

public class UpdateWardDto : UpdateBaseDtos
{
    [StringLength(10, ErrorMessage = "Ward_ZoneNo_MaxLen_10")]
    public string WardNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ward_ZoneNo_Required")]
    public int ZoneId { get; set; } 

    [StringLength(50, ErrorMessage = "Ward_Description_MaxLen_50")]
    public string? Description { get; set; }

    [Range(1, 999, ErrorMessage = "Ward_SequenceNo_MaxValue")]
    public int? SequenceNo { get; set; }
}

