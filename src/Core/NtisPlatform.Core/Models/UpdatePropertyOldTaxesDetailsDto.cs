using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for updating property old taxes details
/// Supports bulk update of tax data across multiple years
/// </summary>
public class UpdatePropertyOldTaxesDetailsDto
{
    [Required]
    public List<UpdateOldTaxYearDto> TaxYears { get; init; } = new();
}

/// <summary>
/// Represents tax data to update for a single finance year
/// </summary>
public class UpdateOldTaxYearDto
{
    public int FinanceYearId { get; set; }
    public string? RVorCV { get; set; }
    public decimal? RVorCVValue { get; set; }

    /// <summary>
    /// List of taxes to update with their amounts
    /// </summary>
    [Required]
    public List<UpdateTaxDetailDto> Taxes { get; init; } = new();
}

/// <summary>
/// Represents a single tax to update
/// </summary>
public class UpdateTaxDetailDto
{
    public int TaxId { get; set; }
    public decimal TaxAmount { get; set; }
}
