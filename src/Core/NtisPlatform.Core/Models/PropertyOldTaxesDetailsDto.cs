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
/// Represents old tax data for a single finance year.
/// When no TransMastOld records exist for the property, FinanceYearId, Year, and YearCode will be null,
/// allowing the user to select a year and save new records.
/// </summary>
public class OldTaxYearDto
{
    /// <summary>
    /// The finance year ID. Null when no TransMastOld records exist for the property.
    /// </summary>
    public int? FinanceYearId { get; set; }

    /// <summary>
    /// The year number. Null when no TransMastOld records exist for the property.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The year code (e.g., "2022-23"). Null when no TransMastOld records exist for the property.
    /// </summary>
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