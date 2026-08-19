namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a village master entity.
/// </summary>
public class VillageMasterEntity : BaseEntity
{
    public int ZoneId { get; set; }
    public string? VillageName { get; set; }
    public string? VillageNameEnglish { get; set; }
    public string? Pincode { get; set; }

    public ZoneEntity Zone { get; set; } = null!;
}
