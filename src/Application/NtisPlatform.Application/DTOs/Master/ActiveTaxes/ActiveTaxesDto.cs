using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class ActiveTaxesDto : BaseDtos
{
    public int Id { get; set; }
    public string? TaxName { get; set; }
    public string? TaxNameAlias { get; set; }
    public int? DisplayOrder { get; set; }
    public bool TaxOnUnit { get; set; }
}

public class CreateActiveTaxesDto : CreateBaseDtos
{
    private string? _taxName;
    private string? _taxNameAlias;

    [Required(ErrorMessage = "ActiveTaxes_TaxName_Required")]
    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxName_MaxLen_200")]
    public string? TaxName
    {
        get => _taxName;
        set => _taxName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxNameAlias_MaxLen_200")]
    public string? TaxNameAlias
    {
        get => _taxNameAlias;
        set => _taxNameAlias = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(1, 999, ErrorMessage = "ActiveTaxes_DisplayOrder_Range")]
    public int? DisplayOrder { get; set; }

    public bool TaxOnUnit { get; set; }
}

public class UpdateActiveTaxesDto : UpdateBaseDtos
{
    private string? _taxName;
    private string? _taxNameAlias;

    [Required(ErrorMessage = "ActiveTaxes_TaxName_Required")]
    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxName_MaxLen_200")]
    public string? TaxName
    {
        get => _taxName;
        set => _taxName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(200, ErrorMessage = "ActiveTaxes_TaxNameAlias_MaxLen_200")]
    public string? TaxNameAlias
    {
        get => _taxNameAlias;
        set => _taxNameAlias = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(1, 999, ErrorMessage = "ActiveTaxes_DisplayOrder_Range")]
    public int? DisplayOrder { get; set; }

    public bool TaxOnUnit { get; set; }
}
