namespace NtisPlatform.Core.Entities;

/// <summary>
///  Represents a ConstructionType entity manage building construction type information.
/// </summary>
public class ConstructionTypeEntity :CommonBaseEntity
{
    public string ConstructionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionEnglish { get; set; } = string.Empty;
    public string GroupID { get; set; } = string.Empty;
    public string KeyboardShortCutKey { get; set; } = string.Empty;
    public int? KeyWiseSequence { get; set; } = 0;


}
