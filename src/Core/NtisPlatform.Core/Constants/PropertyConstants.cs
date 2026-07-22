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

    public static class Categories
    {
        public const string Apartment = "Apartment";
    }

    public static class ErrorMessages
    {
        public const string NotFound = "Property not found";
        public const string UpdateFailed = "Failed to update property details";
    }

    public static class SuccessMessages
    {
        public const string PropertyUpdated = "Property details updated successfully";
    }

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

    public static class SearchByCategory
    {
        public static class ErrorMessages
        {
            public const string InvalidSearchCategory = "Invalid SearchCategory. Valid values are 1=ZoneWise, 2=WardWise, 3=BuildingWise, 4=FromToProperty.";
            public const string ZoneIdRequired = "ZoneId is required for Zone Wise search.";
            public const string WardIdRequired = "WardId is required for this search category.";
            public const string PropertyNoRequired = "PropertyNo is required for Building Wise search.";
            public const string PropertyFromRequired = "PropertyFrom is required for Property Range search.";
            public const string InvalidPropertyFromFormat = "PropertyFrom must start with a numeric property number (e.g. '1' or '1-A9').";
            public const string InvalidPropertyToFormat = "PropertyTo must start with a numeric property number (e.g. '1-S2').";
            public const string InvalidPropertyAssessmentStatusIdFormat = "PropertyAssessmentStatusId must be a comma-separated list of integers (e.g. '1,2').";
        }
    }
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