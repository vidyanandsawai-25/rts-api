using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

/// <summary>
/// Query parameters for the property mapping search endpoint.
/// Inherits SearchTerm from BaseQueryParameters as a single unified Google-style search input.
/// </summary>
public class PropertyMapDetailQueryParameters : BaseQueryParameters
{
    // ── Existing: direct ID lookup ────────────────────────────
    public int? PropertyId { get; set; }

    // ── Individual Search Parameters ──────────────────────────
    public string? OldOwnerName { get; set; }
    public string? OldOwnerNameEnglish { get; set; }
    public string? OldMobileNo { get; set; }
    public string? OldAddress { get; set; }
    public string? OldSocietyName { get; set; }
    public string? OldOccupierName { get; set; }
    public string? OldBuilderName { get; set; }
    public string? OldConstructionYear { get; set; }
}
