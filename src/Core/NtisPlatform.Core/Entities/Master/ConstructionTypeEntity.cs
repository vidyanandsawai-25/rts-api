namespace NtisPlatform.Core.Entities;

/// <summary>
///  Represents a ConstructionType entity manage building construction type information.
/// </summary>
public class ConstructionTypeEntity :BaseEntity
{
    public string ConstructionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SearchSequence { get; set; } = 0;
}
