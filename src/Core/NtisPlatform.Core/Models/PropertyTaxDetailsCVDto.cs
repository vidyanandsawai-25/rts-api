namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Tax Details CV - Pivoted structure with TaxName as columns, grouped by PolicyCode
/// Used for the GET /{propertyId}/tax-details-cv API endpoint
/// Data from PolicyTaxDetailsCV joined with TaxMaster
/// </summary>
public class PropertyTaxDetailsCVDto
{
    /// <summary>
    /// Property identifier
    /// </summary>
    public int PropertyId { get; set; }
    
    /// <summary>
    /// List of tax details grouped by PolicyCode
    /// </summary>
    public List<PolicyTaxDetail> Policies { get; set; } = new List<PolicyTaxDetail>();

    /// <summary>
    /// Dynamic dictionary where Key = TaxName and Value = TaxAmount
    /// Taxes are ordered by DisplayOrder from TaxMaster
    /// </summary>
    public Dictionary<string, decimal?> TaxAmounts { get; set; } = new Dictionary<string, decimal?>();
	
	    /// <summary>
    /// Number of properties included in the aggregation (0 for single property)
    /// </summary>
    public int PropertyCount { get; set; }
}

public class PropertyTaxApartmentDetailsCVDto
{
    /// <summary>
    /// Property identifier
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// List of tax amounts ordered by DisplayOrder
    /// </summary>
    public List<TaxAmountDto> TaxAmounts { get; set; } = new List<TaxAmountDto>();

    /// <summary>
    /// Number of properties included in the aggregation (1 for single property)
    /// </summary>
    public int PropertyCount { get; set; }
}
