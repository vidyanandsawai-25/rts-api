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
}
