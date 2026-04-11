namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents the details of a rate section, including its section number and associated ward.
/// </summary>
public class RateSectionDetailsEntity : BaseEntity
{
    public int RateSectionId { get; set; }
    public int WardId { get; set; }

    // Navigation properties
    public virtual WardEntity? Ward { get; set; }
}

