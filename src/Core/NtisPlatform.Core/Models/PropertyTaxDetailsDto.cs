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

    public Dictionary<string, decimal?> TaxAmounts { get; set; } = new Dictionary<string, decimal?>();

    /// <summary>
    /// Number of properties included in the aggregation (0 for single property)
    /// </summary>
    public int PropertyCount { get; set; }
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
    /// Policy name from PolicyCodeMaster
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// List of individual tax amounts
    /// </summary>
    public List<TaxAmountDetail> TaxAmounts { get; set; } = new List<TaxAmountDetail>();
    
    /// <summary>
    /// Total sum of all tax amounts
    /// </summary>
    public decimal TaxTotal { get; set; }

    /// <summary>
    /// Year-wise retro pending tax details breakdown joined with YearMaster (YearCode)
    /// </summary>
    public List<PendingYearTaxDetail> PendingYears { get; set; } = new List<PendingYearTaxDetail>();
}

/// <summary>
/// Pending tax details for a specific financial year (from TaxPendingDetailsRetro + YearMaster)
/// </summary>
public class PendingYearTaxDetail
{
    public int PendingYearId { get; set; }
    public string YearCode { get; set; } = string.Empty;

    /// <summary>
    /// Which certificate (OC/CC/ELECTRIC_BILL) actually governed THIS specific pending year,
    /// resolved independently per year rather than inherited from whichever policy group this
    /// row happens to be attached to on the grid.
    /// </summary>
    public string PolicyCode { get; set; } = string.Empty;
    public List<TaxAmountDetail> TaxAmounts { get; set; } = new List<TaxAmountDetail>();
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


public class PropertyTaxApartmentDetailsDto
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

/// <summary>
/// DTO for a single tax amount with display order
/// </summary>
public class TaxAmountDto
{
    public string TaxName { get; set; } = string.Empty;
    public decimal? TaxAmount { get; set; }
    public int DisplayOrder { get; set; }
}
