using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Helper class for policy-based area selection in Rateable Value calculations.
/// Provides reusable methods for selecting the appropriate area value based on
/// the current policy configuration (area type × area unit).
/// </summary>
public static class RateableValuePolicyHelper
{
    /// <summary>
    /// Returns a dictionary mapping each property-detail ID to its selected area
    /// value, based on the supplied policy options.
    /// More efficient than calling <see cref="GetSelectedArea(PropertyDetailsEntity,RateableValuePolicyOptions)"/>
    /// per detail because the area-selector function is resolved only once.
    /// </summary>
    public static Dictionary<int, decimal> GetSelectedAreasForProperty(
        IReadOnlyList<PropertyDetailsEntity> details,
        RateableValuePolicyOptions policyOptions)
    {
        var options = policyOptions ?? RateableValuePolicyOptions.Default;
        return GetSelectedAreasForProperty(details, options.AreaType, options.AreaUnit);
    }

    /// <summary>
    /// Returns a dictionary mapping each property-detail ID to its selected area value.
    /// </summary>
    public static Dictionary<int, decimal> GetSelectedAreasForProperty(
        IReadOnlyList<PropertyDetailsEntity> details,
        string areaType,
        string areaUnit)
    {
        if (details == null) throw new ArgumentNullException(nameof(details));

        var normalizedAreaType = areaType?.Trim() ?? RateableValuePolicyConstants.DefaultAreaType;
        var normalizedAreaUnit = areaUnit?.Trim()  ?? RateableValuePolicyConstants.DefaultAreaUnit;

        Func<PropertyDetailsEntity, decimal> areaSelector = GetAreaSelector(normalizedAreaType, normalizedAreaUnit);

        return details.ToDictionary(d => d.Id, d => areaSelector(d));
    }

    /// <summary>
    /// Returns the selected area for a single property detail using policy options.
    /// For bulk operations prefer <see cref="GetSelectedAreasForProperty(List{PropertyDetailsEntity},RateableValuePolicyOptions)"/>.
    /// </summary>
    public static decimal GetSelectedArea(PropertyDetailsEntity detail, RateableValuePolicyOptions policyOptions)
    {
        var options = policyOptions ?? RateableValuePolicyOptions.Default;
        return GetSelectedArea(detail, options.AreaType, options.AreaUnit);
    }

    /// <summary>
    /// Returns the selected area for a single property detail.
    /// </summary>
    public static decimal GetSelectedArea(PropertyDetailsEntity detail, string areaType, string areaUnit)
    {
        if (detail == null) throw new ArgumentNullException(nameof(detail));
        var normalizedAreaType = areaType?.Trim() ?? RateableValuePolicyConstants.DefaultAreaType;
        var normalizedAreaUnit = areaUnit?.Trim()  ?? RateableValuePolicyConstants.DefaultAreaUnit;
        return GetAreaSelector(normalizedAreaType, normalizedAreaUnit)(detail);
    }

    private static Func<PropertyDetailsEntity, decimal> GetAreaSelector(string areaType, string areaUnit)
    {
        if (string.Equals(areaType, RateableValuePolicyConstants.CarpetArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqMeter, StringComparison.OrdinalIgnoreCase))
            return d => Convert.ToDecimal(d.CarpetAreaSqMeter ?? 0d);

        if (string.Equals(areaType, RateableValuePolicyConstants.CarpetArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase))
            return d => Convert.ToDecimal(d.CarpetAreaSqFeet ?? 0d);

        if (string.Equals(areaType, RateableValuePolicyConstants.BuiltupArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqMeter, StringComparison.OrdinalIgnoreCase))
            return d => Convert.ToDecimal(d.BuiltupAreaSqMeter ?? 0d);

        if (string.Equals(areaType, RateableValuePolicyConstants.BuiltupArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase))
            return d => Convert.ToDecimal(d.BuiltupAreaSqFeet ?? 0d);

        // Fallback: CarpetAreaSqMeter
        return d => Convert.ToDecimal(d.CarpetAreaSqMeter ?? 0d);
    }
}
