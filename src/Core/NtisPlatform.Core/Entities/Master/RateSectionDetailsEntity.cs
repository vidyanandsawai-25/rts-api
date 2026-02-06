namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents the details of a rate section, including its section number and associated ward.
/// </summary>
public class RateSectionDetailsEntity : CommonBaseEntity
{
    public int RateSectionDetailsID { get; set; }
    public string RateSectionNo { get; set; } = string.Empty;
    public string WardNo { get; set; } = string.Empty;
}

