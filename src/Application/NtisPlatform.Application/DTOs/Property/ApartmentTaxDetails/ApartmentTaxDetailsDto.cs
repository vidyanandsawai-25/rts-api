namespace NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

/// <summary>
/// Tax details for an apartment (Ward + PropertyNo), optionally narrowed to one wing.
/// <see cref="WingName"/>/<see cref="WingNo"/> are populated only when the result resolves to a
/// single wing - either because <see cref="WingMasterId"/> was supplied, or because every
/// matched unit happens to belong to the same wing.
/// </summary>
public sealed class ApartmentTaxDetailsDto
{
    public int WardId { get; set; }
    public string PropertyNo { get; set; } = null!;
    public int? WingMasterId { get; set; }
    public string? WingName { get; set; }
    public string? WingNo { get; set; }
    public string? SocietyName { get; set; }

    /// <summary>Number of PropertyMast rows (units) whose tax was summed into this result.</summary>
    public int PropertyCount { get; set; }

    /// <summary>One entry per requested valuation method (RV, CV, or both for Dual).</summary>
    public List<CurrentTaxByTypeDto> CurrentTaxes { get; set; } = new();

    /// <summary>Retro-demand tax heads from ptis.TransMast for closed (non-active) finance years.</summary>
    public List<TaxHeadAmountDto> Arrears { get; set; } = new();
}
