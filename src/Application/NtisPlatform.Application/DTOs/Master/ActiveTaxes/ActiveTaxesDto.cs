using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class ActiveTaxesDto : CommonBaseDtos
{
    public int TaxNameID { get; set; }
    public string? TaxName { get; set; } = string.Empty;
    public string? TaxNameAlias { get; set; }
    public int? TaxNameOrder { get; set; }
    public bool? ActiveTaxHeadsOnly { get; set; }
    public int? DisplayOrder { get; set; }
}

public class CreateActiveTaxesDto : CreateCommonBaseDtos
{
    [Required(ErrorMessage = "ActiveTaxes_TaxName_Required")]
    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxName_MaxLen_200")]
    public string TaxName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxNameAlias_MaxLen_200")]
    public string? TaxNameAlias { get; set; }

    [Range(1, 999, ErrorMessage = "ActiveTaxes_TaxNameOrder_Range")]
    public int? TaxNameOrder { get; set; }

    public bool? ActiveTaxHeadsOnly { get; set; }

    [Range(1, 999, ErrorMessage = "ActiveTaxes_DisplayOrder_Range")]
    public int? DisplayOrder { get; set; }
}

public class UpdateActiveTaxesDto : UpdateCommonBaseDtos
{
    [Required(ErrorMessage = "ActiveTaxes_TaxName_Required")]
    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxName_MaxLen_200")]
    public string TaxName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxNameAlias_MaxLen_200")]
    public string? TaxNameAlias { get; set; }

    [Range(1, 999, ErrorMessage = "ActiveTaxes_TaxNameOrder_Range")]
    public int? TaxNameOrder { get; set; }

    public bool? ActiveTaxHeadsOnly { get; set; }

    [Range(1, 999, ErrorMessage = "ActiveTaxes_DisplayOrder_Range")]
    public int? DisplayOrder { get; set; }
}
