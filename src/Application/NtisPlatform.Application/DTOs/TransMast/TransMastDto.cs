using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TransMastDto : BaseDtos
{
    public int PropertyId { get; set; }
    public int FinanceYearId { get; set; }
    public int? FinanceYear { get; set; }
    public string RVorCV { get; set; } = string.Empty;
    public decimal RVorCVValue { get; set; }
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public decimal TaxAmount { get; set; }
    public bool MarkedForDeletion { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreateTransMastDto : CreateBaseDtos
{
    public int PropertyId { get; set; }
    public int FinanceYearId { get; set; }
    public string RVorCV { get; set; } = string.Empty;
    public decimal RVorCVValue { get; set; }
    public int TaxId { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class UpdateTransMastDto : UpdateBaseDtos
{
    public string? RVorCV { get; set; }
    public decimal? RVorCVValue { get; set; }
    public decimal? TaxAmount { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class TransMastQueryParameters : BaseQueryParameters
{
    public int? PropertyId { get; set; }
    public int? FinanceYearId { get; set; }
    public int? TaxId { get; set; }
    public string? RVorCV { get; set; }
    public bool? IsActive { get; set; }
}
