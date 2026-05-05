using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class TaxZoningDto 
{
    public int? TaxZoneId { get; set; }
    public string? TaxZoneNo { get; set; } = string.Empty;
    public int? WardId { get; set; }
    public string? WardNo { get; set; } = string.Empty;
    public string? PropertyNo { get; set; } = string.Empty;
    public string? FromProperty { get; set; } = string.Empty;
    public string? ToProperty { get; set; } = string.Empty;

}

public class UpdateTaxZoningDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TaxZoneID_required")]
    [Range(1, int.MaxValue, ErrorMessage = "TaxZoneId_must_be_positive")]
    public int TaxZoneId { get; set; }

    [Required(ErrorMessage = "WardID_required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardId_must_be_positive")]
    public int WardId { get; set; }

    public string? PropertyNo { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "FromProperty_maximum_length_exceeded")]
    public string? FromProperty { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "ToProperty_maximum_length_exceeded")]
    public string? ToProperty { get; set; } = string.Empty;
}