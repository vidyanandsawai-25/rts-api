namespace NtisPlatform.Core.Constants;

public static class PropertyConstants
{
    public static readonly IReadOnlyList<string> ApartmentCategoryNames = new[]
    {
        "Apartment",
        "Multi Commercial Apartment"
    };
    
}
public static class PartType
{
    /// <summary>
    /// Name of the property part type used in PropertyTypeMaster
    /// </summary>
    public const string Aminity = "Aminity";
    public const string Residential = "R";
    public const string Commercial   = "C";
}