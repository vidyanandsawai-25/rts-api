using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Constants;

public static class PropertyConstants
{
    public static readonly IReadOnlyList<string> ApartmentCategoryNames = new[]
    {
        "Apartment",
        "Multi Commercial Apartment"
    };

    /// <summary>
    /// Expression to filter out blank/empty partitions and single-letter partitions.
    /// Only keeps partitions containing at least one digit (e.g. C1, C2, A1, B2, 1, 2, 3...)
    /// </summary>
    public static readonly Expression<Func<PropertyEntity, bool>> HasDigitInPartition =
        x => x.PartitionNo != null && x.PartitionNo != "" && (
            x.PartitionNo.Contains("0") ||
            x.PartitionNo.Contains("1") ||
            x.PartitionNo.Contains("2") ||
            x.PartitionNo.Contains("3") ||
            x.PartitionNo.Contains("4") ||
            x.PartitionNo.Contains("5") ||
            x.PartitionNo.Contains("6") ||
            x.PartitionNo.Contains("7") ||
            x.PartitionNo.Contains("8") ||
            x.PartitionNo.Contains("9")
        );
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