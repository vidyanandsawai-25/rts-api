namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a tax zone entity that manages tax zone information.
/// </summary>
public class TaxZoneEntity : BaseEntity
{
    public int Id { get; set; }

    public string TaxZoneNo { get; set; } = null!;

    public string? TaxZoneType { get; set; }

    public string Remark { get; set; } = null!;
}
