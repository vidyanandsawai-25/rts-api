using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TransMastDto : BaseDtos
{
    public int PropertyId { get; set; }
    public int FinanceYearId { get; set; }
    public int? FinanceYear { get; set; }
    public string CalculationType { get; set; } = string.Empty;
    public decimal CalculationValue { get; set; }
    public string RVorCV
    {
        get => CalculationType;
        set => CalculationType = value;
    }
    public decimal RVorCVValue
    {
        get => CalculationValue;
        set => CalculationValue = value;
    }
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
    public string CalculationType { get; set; } = string.Empty;
    public decimal CalculationValue { get; set; }
    public string RVorCV
    {
        get => CalculationType;
        set => CalculationType = value;
    }
    public decimal RVorCVValue
    {
        get => CalculationValue;
        set => CalculationValue = value;
    }
    public int TaxId { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class UpdateTransMastDto : UpdateBaseDtos
{
    public string? CalculationType { get; set; }
    public decimal? CalculationValue { get; set; }
    public string? RVorCV
    {
        get => CalculationType;
        set => CalculationType = value;
    }
    public decimal? RVorCVValue
    {
        get => CalculationValue;
        set => CalculationValue = value;
    }
    public decimal? TaxAmount { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class TransMastQueryParameters : BaseQueryParameters
{
    public int? PropertyId { get; set; }
    public int? FinanceYearId { get; set; }
    public int? TaxId { get; set; }
    public string? CalculationType { get; set; }
    public string? RVorCV
    {
        get => CalculationType;
        set => CalculationType = value;
    }
    public bool? IsActive { get; set; }
}
