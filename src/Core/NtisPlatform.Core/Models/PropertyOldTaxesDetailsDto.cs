namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Old Taxes Details - represents tax data for a single finance year
/// Includes dynamic tax columns based on active old taxes from TaxMaster
/// </summary>
public class PropertyOldTaxesDetailsDto
{
    public int PropertyId { get; set; }
    public List<OldTaxYearDto> TaxYears { get; set; } = new();
}

/// <summary>
/// Represents old tax data for a single finance year
/// </summary>
public class OldTaxYearDto
{
    public int FinanceYearId { get; set; }
    public int Year { get; set; }
    public string? YearCode { get; set; }

    /// <summary>
    /// List of taxes with their names and amounts
    /// </summary>
    public List<TaxDetailDto> Taxes { get; set; } = new();
}

/// <summary>
/// Represents a single tax with its name and amount
/// </summary>
public class TaxDetailDto
{
    public int TaxId { get; set; }
    public string TaxName { get; set; } = null!;
    public decimal TaxAmount { get; set; }
}