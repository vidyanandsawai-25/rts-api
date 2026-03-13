namespace NtisPlatform.Core.Entities;

public class RateSectionEntity : BaseEntity
{
    public string RateSectionNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEnglish { get; set; }
}

