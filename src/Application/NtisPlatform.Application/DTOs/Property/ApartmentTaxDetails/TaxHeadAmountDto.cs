namespace NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

/// <summary>
/// One tax head's amount for one policy code, already summed across every matched property.
/// <see cref="PolicyCode"/> is the PolicyCodeMaster.PolicyCode for rows sourced from
/// PolicyTaxDetails/PolicyTaxDetailsCV (e.g. "NETTAX", "OC"), or the literal "Net Pay" for rows
/// sourced from the current finance year's ptis.TransMast - the real PolicyCodeMaster.PolicyCode
/// is not surfaced for those, since they represent the year's net payable rather than a specific
/// policy stage. Arrears rows keep their real PolicyCode since they are historical.
/// </summary>
public sealed class TaxHeadAmountDto
{
    public int TaxId { get; set; }
    public string TaxName { get; set; } = null!;
    public string PolicyCode { get; set; } = null!;
    public decimal TaxAmount { get; set; }
}
