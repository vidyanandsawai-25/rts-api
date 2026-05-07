using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class TaxZoneDto : BaseDtos
{
    public int Id { get; set; }
    public string TaxZoneNo { get; set; } = null!;
    public string? TaxZoneType { get; set; }
    public string Remark { get; set; } = null!;
}

public class CreateTaxZoneDto : CreateBaseDtos
{
    private string _taxZoneNo = string.Empty;
    private string? _taxZoneType;
    private string _remark = string.Empty;

    [Required(ErrorMessage = "TaxZoneNo_Required")]
    [StringLength(10)]
    public string TaxZoneNo
    {
        get => _taxZoneNo;
        set => _taxZoneNo = value?.Trim() ?? string.Empty;
    }


    [Required(ErrorMessage = "TaxZoneType_Required")]
    [StringLength(50, ErrorMessage = "TaxZoneType_MaxLen_50")]
    public string? TaxZoneType
    {
        get => _taxZoneType;
        set => _taxZoneType = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(50, ErrorMessage = "Remark_MaxLen_50")]
    public string? Remark
    {
        get => _remark;
        set => _remark = value?.Trim() ?? string.Empty;
    }
}

public class UpdateTaxZoneDto : UpdateBaseDtos
{
    private string _taxZoneNo = string.Empty;
    private string _taxZoneType = string.Empty;
    private string _remark = string.Empty;

    [Required(ErrorMessage = "TaxZoneNo_Required")]
    [StringLength(10)]
    public string TaxZoneNo
    {
        get => _taxZoneNo;
        set => _taxZoneNo = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "TaxZoneType_Required")]
    [StringLength(50, ErrorMessage = "TaxZoneType_MaxLen_50")]
    public string TaxZoneType
    {
        get => _taxZoneType;
        set => _taxZoneType = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [StringLength(50, ErrorMessage = "Remark_MaxLen_50")]
    public string? Remark
    {
        get => _remark;
        set => _remark = value?.Trim() ?? string.Empty;
    }
}
