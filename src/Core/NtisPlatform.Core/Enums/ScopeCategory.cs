using System.Collections.Generic;
using System.ComponentModel;

namespace NtisPlatform.Core.Enums;

/// <summary>
/// Categories of scope selection for property operations.
/// </summary>
public enum ScopeCategory
{
    [Description("Zone / Node")]
    ZoneNode = 1,

    [Description("Ward / Sector")]
    WardSector = 2,

    [Description("Building Wise")]
    BuildingWise = 3,

    [Description("Property Wise")]
    PropertyWise = 4,

    [Description("Property Range")]
    PropertyRange = 5
}

/// <summary>
/// Extension methods for ScopeCategory enum mapping.
/// </summary>
public static class ScopeCategoryExtensions
{
    public static string GetDisplayName(this ScopeCategory category)
    {
        return category switch
        {
            ScopeCategory.ZoneNode => "Zone / Node",
            ScopeCategory.WardSector => "Ward / Sector",
            ScopeCategory.BuildingWise => "Building Wise",
            ScopeCategory.PropertyWise => "Property Wise",
            ScopeCategory.PropertyRange => "Property Range",
            _ => category.ToString()
        };
    }

    public static string GetDescription(this ScopeCategory category)
    {
        return category switch
        {
            ScopeCategory.ZoneNode => "Zone-wise selection",
            ScopeCategory.WardSector => "Multi ward selection",
            ScopeCategory.BuildingWise => "Building level",
            ScopeCategory.PropertyWise => "Property level",
            ScopeCategory.PropertyRange => "From-to property range",
            _ => string.Empty
        };
    }

    public static List<string> GetOptions(this ScopeCategory category)
    {
        return category switch
        {
            ScopeCategory.ZoneNode => new List<string> { "Zone", "Property Type", "Assessment Status" },
            ScopeCategory.WardSector => new List<string> { "Zone", "Ward", "Property Type", "Assessment Status" },
            ScopeCategory.BuildingWise => new List<string> { "Zone", "Ward", "Property No" },
            ScopeCategory.PropertyWise => new List<string> { "UPIC Id", "Mobile No" },
            ScopeCategory.PropertyRange => new List<string> { "Ward", "From Property", "To Property" },
            _ => new List<string>()
        };
    }
    public static string GetScopeType(this ScopeCategory category)
    {
        return category switch
        {
            ScopeCategory.ZoneNode => "zone",
            ScopeCategory.WardSector => "ward",
            ScopeCategory.BuildingWise => "building",
            ScopeCategory.PropertyWise => "property",
            ScopeCategory.PropertyRange => "range",
            _ => category.ToString()
        };
    }
}
