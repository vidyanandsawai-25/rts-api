namespace NtisPlatform.Core.Constants;

public static class PropertyConstants
{
    public static readonly IReadOnlyList<string> ApartmentCategoryNames = new[]
    {
        "Apartment",
        "Multi Commercial Apartment"
    };
    
}
public static class PartTypeConstants
{
    /// <summary>
    /// Name of the property part type used in PropertyTypeMaster
    /// </summary>
    public const string Amenity = "Amenity";
    public const string Residential = "R";
    public const string Commercial = "C";
    public const string Plot = "Plot";
}
public static class PartitionNoConstants
{
    /// <summary>
    /// Name of the property part type used in PropertyTypeMaster
    /// </summary>
    public const string AmenityPartitionNo = "AM";
}