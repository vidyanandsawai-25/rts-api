using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>A single value-based percentage row (RV base) keyed by type-of-use + year range.</summary>
public class ValueBasedTaxRowDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_TypeOfUseId_Invalid")]
    public int TypeOfUseId { get; set; }

    public string? TypeOfUseCode { get; set; }
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_YearRangeRVId_Invalid")]
    public int YearRangeRVId { get; set; }
    /// <summary>User group (R / C / I …) derived from TypeOfUse.Type.</summary>
    public string? UserGroup { get; set; }
    /// <summary>Base the percentage applies to. Currently RV (ALV/CV handled by the CV path).</summary>
    public string BaseType { get; set; } = "RV";

    [Range(0, 999, ErrorMessage = "ValueBasedTax_Percentage_OutOfRange")]
    public decimal TaxPercentage { get; set; }
}

/// <summary>Persist explicit per-row percentage edits (insert or update).</summary>
public class SaveValueBasedTaxRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_YearRangeRVId_Invalid")]
    public int YearRangeRVId { get; set; }

    /// <summary>
    /// Base Type is a tax+year-wide setting (one shared RV/ALV choice), not independent
    /// per row — applied to EVERY row for this tax+year, not just the rows in <see cref="Rows"/>
    /// (which is typically only the currently-loaded page under server-side pagination).
    /// </summary>
    [Required]
    public string BaseType { get; set; } = "RV";

    public int? UpdatedBy { get; set; }

    [MinLength(1, ErrorMessage = "ValueBasedTax_Rows_Required")]
    public List<ValueBasedTaxRowDto> Rows { get; set; } = new();
}

/// <summary>Apply one percentage across all rows of a tax + year range (optionally a user group).</summary>
public class BulkApplyValueBasedTaxRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValueBasedTax_YearRangeRVId_Invalid")]
    public int YearRangeRVId { get; set; }

    /// <summary>Optional user-group prefix filter (e.g. "R", "C", "I"). Null = all.</summary>
    public string? UserGroup { get; set; }

    [Required]
    [Range(0, 999, ErrorMessage = "ValueBasedTax_Percentage_OutOfRange")]
    public decimal TaxPercentage { get; set; }

    public int? UpdatedBy { get; set; }
}
