namespace NtisPlatform.Core.Entities;

public class SubTypeOfUseEntity : BaseEntity
{
  
    public string? Description { get; set; } = string.Empty;
    public int TypeOfUseId { get; set; } 
    public int? SearchSequence { get; set; }
}

