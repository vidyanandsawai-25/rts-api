namespace NtisPlatform.Core.Entities;

public class TypeOfUseEntity : CommonBaseEntity
{
    public string TypeOfUseID { get; set; }
    public string Description { get; set; }
    public string? DescriptionEnglish { get; set; }
    public string Type { get; set; }
    public string GroupID { get; set; }
    public string? SearchKey { get; set; }
    public int? SearchSequence { get; set; }
    public bool? IsSociety { get; set; }
}

