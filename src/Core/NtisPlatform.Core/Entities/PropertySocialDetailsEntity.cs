using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class PropertySocialDetailsEntity : BaseEntity
{
    public int PropertyId { get; set; }
    public int SocialAttributeId { get; set; }
    public bool? BitValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public string? TextValue { get; set; }
    public DateTime? DateValue { get; set; }
    public string? Remark { get; set; }

    // Navigation properties
    public PropertyEntity? Property { get; set; }
    public SocialAttributeEntity? SocialAttribute { get; set; }
}