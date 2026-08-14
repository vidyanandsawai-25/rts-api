using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Bulk import/validate request for the "Validate &amp; Update" bulk drawer flow. The response is
/// the platform's existing generic <c>RangeResult&lt;TaxZoningRangeDto&gt;</c>
/// (<c>Application.DTOs.Range</c>) — reused as-is since it is not TaxZoning-specific.
/// </summary>
public class BulkTaxZoningRangeRequest
{
    [Required(ErrorMessage = "TaxZoningRange_Bulk_Items_Required")]
    [MinLength(1, ErrorMessage = "TaxZoningRange_Bulk_Items_Required")]
    public List<CreateTaxZoningRangeDto> Items { get; set; } = new();
}
