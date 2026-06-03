using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RateSectionEntity : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public ICollection<RateEntity> Rates { get; set; } = new List<RateEntity>();
    public ICollection<RateSectionDetailsEntity> RateSectionDetails { get; set; } = new List<RateSectionDetailsEntity>();
}

