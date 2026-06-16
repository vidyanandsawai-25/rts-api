namespace NtisPlatform.Application.DTOs.TaxApplicability;

/// <summary>
/// Request DTO to update tax applicability for a property.
/// </summary>
public class UpdateTaxApplicabilityRequestDto
{
    /// <summary>
    /// Property identifier
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// List of taxes with their active status
    /// </summary>
    public List<UpdateTaxStatusDto> Taxes { get; set; } = new();

    /// <summary>
    /// User identifier performing the update
    /// </summary>
    public int? UserId { get; set; }
}

/// <summary>
/// Detail DTO representing individual tax applicability status for updates
/// </summary>
public class UpdateTaxStatusDto
{
    /// <summary>
    /// Tax identifier
    /// </summary>
    public int TaxId { get; set; }

    /// <summary>
    /// Applicability flag. True if applicable, False if disabled/exempted.
    /// </summary>
    public bool IsApplicable { get; set; }
}
