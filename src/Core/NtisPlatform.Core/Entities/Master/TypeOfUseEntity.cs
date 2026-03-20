namespace NtisPlatform.Core.Entities;

public class TypeOfUseEntity : BaseEntity
{
    public int TypeOfUseId { get; set; }
    public string TypeOfUseCode { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public int TypeOfUseGroupId { get; set; }
    public string? SearchKey { get; set; }
    public int? SearchSequence { get; set; }
}

