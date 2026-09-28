using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Source input context for mapping an individual New Survey property detail/unit into <see cref="PropertyApartmentTaxDto"/>.
/// </summary>
public record NewSurveyMappingInput(
    JoinedPropertyRowDto Property,
    string? ZoneNo,
    string? WardNo,
    FetchDetailRowDto? Detail,
    PropertyMastOldEntity? OldProperty,
    decimal? RetroTaxTotal,
    decimal? RateableValue = null,
    decimal? CapitalValue = null,
    decimal? CalculationValue = null,
    decimal NewTaxTotal = 0m,
    decimal NewTaxTotalRV = 0m,
    decimal NewTaxTotalCV = 0m,
    decimal? CurrentDemand = null,
    List<PropertyPhotoDocumentDto>? Photos = null
);
