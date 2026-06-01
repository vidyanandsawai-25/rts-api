namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Identifies which single column's distinct values are requested by the filter-options endpoint.
/// Null means "return all columns" (the default / no-field case).
/// </summary>
public enum ApartmentQCFilterColumn
{
    Wing,
    ApartmentType,
    FlatOrShopNo,
    PropertyType
}
