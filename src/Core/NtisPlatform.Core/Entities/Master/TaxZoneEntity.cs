using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a tax zone entity that manages tax zone information.
/// </summary>
public class TaxZoneEntity : BaseEntity
{
    public string TaxZoneNo { get; set; } = null!;

    public string? TaxZoneType { get; set; }

    public string Remark { get; set; } = null!;
    public ICollection<RateEntity> Rates { get; set; } = new List<RateEntity>();
    public ICollection<PropertyEntity> Property { get; set; } = new List<PropertyEntity>();
}
