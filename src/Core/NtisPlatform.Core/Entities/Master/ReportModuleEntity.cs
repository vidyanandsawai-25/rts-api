namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// A report "module" (display name + optional logo image), owned and maintained by the
/// separate report-admin tool. Lives in the report queue database (dbo.Module).
///
/// Deliberately does NOT inherit BaseEntity: dbo.Module has no CreatedBy/UpdatedBy/IsActive
/// columns, only Id/Name/Logo*/CreatedDate/UpdatedDate. Read-only from this app's perspective —
/// modules are created/edited/deleted exclusively through the report-admin tool.
/// </summary>
public class ReportModuleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoFileName { get; set; }
    public string? LogoContentType { get; set; }
    public byte[]? LogoContent { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
