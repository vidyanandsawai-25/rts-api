using System.Collections.Generic;
using System.ComponentModel;

namespace NtisPlatform.Core.Enums;

/// <summary>
/// Categories of scope selection for property operations.
/// </summary>
public enum ScopeCategory
{
    [Description("All Properties")]
    AllProperties = 1,

    [Description("Zone / Node")]
    ZoneNode = 2,

    [Description("Ward / Sector")]
    WardSector = 3,

    [Description("Building Wise")]
    BuildingWise = 4,

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
            ScopeCategory.AllProperties => "All Properties",
            ScopeCategory.ZoneNode => "Zone / Node",
            ScopeCategory.WardSector => "Ward / Sector",
            ScopeCategory.BuildingWise => "Building Wise",
            ScopeCategory.PropertyRange => "Property Range",
            _ => category.ToString()
        };
    }

    public static string GetDescription(this ScopeCategory category)
    {
        return category switch
        {
            ScopeCategory.AllProperties => "Entire corporation",
            ScopeCategory.ZoneNode => "Zone-wise selection",
            ScopeCategory.WardSector => "Multi ward selection",
            ScopeCategory.BuildingWise => "Building level",
            ScopeCategory.PropertyRange => "From-to property range",
            _ => string.Empty
        };
    }

    public static List<string> GetOptions(this ScopeCategory category)
    {
        return category switch
        {
            ScopeCategory.AllProperties => new List<string>(),
            ScopeCategory.ZoneNode => new List<string> { "Zone", "Property Type" },
            ScopeCategory.WardSector => new List<string> { "Zone", "Ward", "Property Type" },
            ScopeCategory.BuildingWise => new List<string> { "Zone", "Ward", "Property No" },
            ScopeCategory.PropertyRange => new List<string> { "Ward", "From Property", "To Property" },
            _ => new List<string>()
        };
    }
}
