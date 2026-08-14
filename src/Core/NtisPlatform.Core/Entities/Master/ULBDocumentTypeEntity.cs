namespace NtisPlatform.Core.Entities;

/// <summary>
/// Lookup row for a category of ULB-wide document (e.g. Tax Zoning List/Map, Ready Reckoner Rate
/// Chart, Tax Rate Chart). Seeded/maintained directly in <c>PTIS.ULBDocumentType</c>; the
/// application only reads from this table.
/// </summary>
public class ULBDocumentTypeEntity : BaseEntity
{
    public string DocumentTypeCode { get; set; } = string.Empty;

    public string DocumentTypeName { get; set; } = string.Empty;
}
