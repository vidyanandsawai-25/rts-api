namespace NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

/// <summary>
/// Which valuation method's current tax to read: Rateable Value (ptis.PolicyTaxDetails),
/// Capital Value (ptis.PolicyTaxDetailsCV), or both.
/// </summary>
public enum ApartmentTaxType
{
    RV,
    CV,
    Dual
}
