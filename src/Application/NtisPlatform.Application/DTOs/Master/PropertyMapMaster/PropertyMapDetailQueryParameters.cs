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
}
