namespace NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

/// <summary>Current tax-head totals for one valuation method ("RV" or "CV").</summary>
public sealed class CurrentTaxByTypeDto
{
    public string TaxType { get; set; } = null!;
    public List<TaxHeadAmountDto> TaxHeads { get; set; } = new();
}
