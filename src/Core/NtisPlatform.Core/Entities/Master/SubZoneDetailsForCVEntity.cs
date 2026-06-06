using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities.Master;

public class SubZoneDetailsForCVEntity : BaseEntity
{
    public int MoujaId { get; set; }

    public string SubZoneNo { get; set; } = string.Empty;

    public string SubZoneName { get; set; } = string.Empty;

    // Navigation property
    public MoujaEntity? Mouja { get; set; }
}
