using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class TaxZoneDto : CommonBaseDtos
{
    public string TaxZoneNo { get; set; } = null!;
    public string? TaxZoneType { get; set; }
    public string Remark { get; set; } = null!;
}

public class CreateTaxZoneDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "TaxZoneNo_Required")]
    [StringLength(10, ErrorMessage = "TaxZoneNo_MaxLen_10")]  
    public string TaxZoneNo { get; set; } = null!;

    [StringLength(50, ErrorMessage = "TaxZoneType_MaxLen_50")] 
    public string? TaxZoneType { get; set; }

    [Required(ErrorMessage = "Remark_Required")]
    [StringLength(50, ErrorMessage = "Remark_MaxLen_50")] 
    public string Remark { get; set; } = null!;
}

public class UpdateTaxZoneDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "TaxZoneNo_Required")]
    [StringLength(10, ErrorMessage = "TaxZoneNo_MaxLen_10")]  
    public string TaxZoneNo { get; set; } = null!;

    [StringLength(50, ErrorMessage = "TaxZoneType_MaxLen_50")]  
    public string? TaxZoneType { get; set; }

    [Required(ErrorMessage = "Remark_Required")]
    [StringLength(50, ErrorMessage = "Remark_MaxLen_50")]  
    public string Remark { get; set; } = null!;
}
