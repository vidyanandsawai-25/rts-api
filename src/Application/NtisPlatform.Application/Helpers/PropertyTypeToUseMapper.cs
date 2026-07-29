using System;
using System.Collections.Generic;

namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Maps property type codes to use categories
/// </summary>
public static class PropertyTypeToUseMapper
{
    private static readonly Dictionary<string, string> TypeToUseMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "C", "Non Residential" },
        { "I", "Non Residential" },
        { "I-C", "Mixed" },
        { "N", "Non Taxable" },
        { "R", "Residential" },
        { "R-C", "Mixed" }
    };

    /// <summary>
    /// Get the use category for a given property type code
    /// </summary>
    /// <param name="typeCode">Property type code (C, I, I-C, N, R, R-C)</param>
    /// <returns>Use category description</returns>
    public static string GetUseCategory(string? typeCode)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
            return "Unknown";

        return TypeToUseMapping.TryGetValue(typeCode, out var useCategory)
            ? useCategory
            : "Unknown";
    }

    /// <summary>
    /// Check if use has changed between old and new property types
    /// </summary>
    /// <param name="oldTypeCode">Old property type code</param>
    /// <param name="newTypeCode">New property type code</param>
    /// <returns>True if use category changed</returns>
    public static bool HasUseChanged(string? oldTypeCode, string? newTypeCode)
    {
        var oldUse = GetUseCategory(oldTypeCode);
        var newUse = GetUseCategory(newTypeCode);
        return !string.Equals(oldUse, newUse, StringComparison.OrdinalIgnoreCase);
    }
}
