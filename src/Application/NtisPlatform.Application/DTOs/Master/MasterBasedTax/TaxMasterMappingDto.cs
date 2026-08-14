using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>A single master-based mapping row (read + upsert item).</summary>
public class TaxMasterMappingDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MasterBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    public string MasterKey { get; set; } = null!;
    public string? DisplayValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MasterBasedTax_AssessmentYearRangeId_Invalid")]
    public int AssessmentYearRangeId { get; set; }
    public string ResultMode { get; set; } = "FIXED";
    public string ResultBase { get; set; } = "NONE";

    [Range(0, 999, ErrorMessage = "MasterBasedTax_ResultValue_OutOfRange")]
    public decimal ResultValue { get; set; }
}

/// <summary>Persist explicit per-row edits (insert or update by Id/MasterKey).</summary>
public class SaveMasterMappingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "MasterBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }


    [Range(1, int.MaxValue, ErrorMessage = "MasterBasedTax_AssessmentYearRangeId_Invalid")]
    public int AssessmentYearRangeId { get; set; }

    public int? UpdatedBy { get; set; }

    [MinLength(1, ErrorMessage = "MasterBasedTax_Rows_Required")]
    public List<TaxMasterMappingDto> Rows { get; set; } = new();
}

/// <summary>Bulk-apply a single value/mode/base across all rows of a tax + year range.</summary>
public class BulkApplyMasterMappingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "MasterBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MasterBasedTax_AssessmentYearRangeId_Invalid")]
    public int AssessmentYearRangeId { get; set; }

    [Required]
    public string ResultMode { get; set; } = "FIXED";

    [Required]
    public string ResultBase { get; set; } = "NONE";

    [Required]
    [Range(0, 999, ErrorMessage = "MasterBasedTax_ResultValue_OutOfRange")]
    public decimal ResultValue { get; set; }

    public int? UpdatedBy { get; set; }
}
