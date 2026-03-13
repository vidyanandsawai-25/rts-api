namespace NtisPlatform.Core.Entities;

public class SubTypeOfUseEntity : BaseEntity
{
    public int SubTypeOfUseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEnglish { get; set; }
    public string TypeOfUseID { get; set; } = string.Empty;
    public string? SearchKey { get; set; }
    public int? SearchSequence { get; set; }
}

