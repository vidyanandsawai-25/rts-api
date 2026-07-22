namespace NtisPlatform.Core.Enums;

/// <summary>
/// Search scope for <c>PropertySearchByCategory</c> - selects which set of parameters
/// (Zone/Ward/Building/Property-range) is used to scope the property search.
/// </summary>
public enum PropertySearchCategory
{
    ZoneWise = 1,
    WardWise = 2,
    BuildingWise = 3,
    FromToProperty = 4
}
