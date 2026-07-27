using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

public class MoujaEntity : BaseEntity
{
    public string MoujaNo { get; set; } = string.Empty;
    public string MoujaName { get; set; } = string.Empty;
    public ICollection<PropertyEntity> Property { get; set; } = new List<PropertyEntity>();
    public ICollection<CSNDetailsEntity> CSNDetails { get; set; } = new List<CSNDetailsEntity>();
    public ICollection<SubZoneDetailsForCVEntity> SubZoneDetails { get; set; } = new List<SubZoneDetailsForCVEntity>();
}
