namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Tax Details - Pivoted structure with TaxName as columns, grouped by PolicyCode
/// Used for the GET /{propertyId}/tax-details API endpoint
/// </summary>
public class PropertyTaxDetailsDto
{
    /// <summary>
    /// Property identifier
    /// </summary>
    public int PropertyId { get; set; }
    
    /// <summary>
    /// List of tax details grouped by PolicyCode
    /// </summary>
    public List<PolicyTaxDetail> Policies { get; set; } = new List<PolicyTaxDetail>();
}

/// <summary>
/// Tax details for a specific policy code
/// </summary>
public class PolicyTaxDetail
{
    /// <summary>
    /// Policy code identifier
    /// </summary>
    public string PolicyCode { get; set; } = string.Empty;
    
    /// <summary>
    /// List of individual tax amounts
    /// </summary>
    public List<TaxAmountDetail> TaxAmounts { get; set; } = new List<TaxAmountDetail>();
    
    /// <summary>
    /// Total sum of all tax amounts
    /// </summary>
    public decimal TaxTotal { get; set; }
}

/// <summary>
/// Individual tax amount detail
/// </summary>
public class TaxAmountDetail
{
    /// <summary>
    /// Name of the tax
    /// </summary>
    public string TaxName { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount of the tax
    /// </summary>
    public decimal TaxAmount { get; set; }
}
