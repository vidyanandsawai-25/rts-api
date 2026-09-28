using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application-layer service for the ApartmentQC top-section panel.
/// Assembles raw data fetched via <see cref="IApartmentQcTopSectionRepository"/> into the
/// API-facing DTO shape, and delegates the performance-summary formulas to
/// <see cref="ApartmentQcTopSectionPerformanceCalculator"/>.
/// </summary>
public class ApartmentQcTopSectionService : IApartmentQcTopSectionService
{
    private readonly IApartmentQcTopSectionRepository _repository;
    private readonly ApartmentQcTopSectionPerformanceCalculator _calculator;

    public ApartmentQcTopSectionService(
        IApartmentQcTopSectionRepository repository,
        ApartmentQcTopSectionPerformanceCalculator calculator)
    {
        _repository = repository;
        _calculator = calculator;
    }

    public async Task<PropertyTopSectionDto?> GetTopSectionAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default)
    {
        var property = await _repository.GetPropertyAsync(query, cancellationToken);
        if (property is null)
        {
            return null;
        }

        var (carpetFt, carpetMtr) = await _repository.GetCarpetAreaTotalsAsync(property.Id, cancellationToken);
        var (builtUpFt, builtUpMtr) = await _repository.GetBuiltUpAreaTotalsAsync(property.Id, cancellationToken);
        var (plotFt, plotMtr) = await _repository.GetPlotAreaTotalsAsync(property.Id, cancellationToken);
        var (currentTax, retroTax, oldCurrentTax, pendingCurrent) = await _repository.GetAdditionalRevenueTaxDetailsAsync(property.Id, cancellationToken);
        var wings = await _repository.GetSocietyWingsAsync(property.Id, cancellationToken);

        // PartitionNo is left out entirely when blank/null - never defaulted to a literal "0",
        // since a real "0" partition value is meaningful and must stay distinguishable from "no partition".
        var propertyIdParts = new[] { property.WardNo, property.PropertyNo, property.PartitionNo }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var propertyStatus = property.IsLocked ? "Locked" : (property.IsActive ? "Active" : "Not Active");

        var overview = new PropertyOverviewDto
        {
            Upic = property.Upic,
            PropertyId = string.Join("-", propertyIdParts),
            IsActive = property.IsActive,
            IsLocked = property.IsLocked,
            PropertyStatus = propertyStatus,
            SecretaryName = property.SecretaryName,
            SecretaryNameEnglish = property.SecretaryNameEnglish,
            SecretaryMobileNo = property.SecretaryMobileNo,
            SecretaryEmailId = property.SecretaryEmailId,
            ManagerName = property.ManagerName,
            ManagerNameEnglish = property.ManagerNameEnglish,
            ManagerMobileNo = property.ManagerMobileNo,
            ManagerEmailId = property.ManagerEmailId,
            PropertyHolder = property.OwnerName,
            PropertyCategory = property.PropertyCategoryName,
            PropertyDescription = property.PropertyDescription,
            SocietyName = property.SocietyName,
            SocietyNameEnglish = property.SocietyNameEnglish,
            SocietyAddress = property.SocietyAddress,
            SocietyAddressEnglish = property.SocietyAddressEnglish,
            SocietyEmailId = property.SocietyEmailId,
            LandOwnerName = property.LandOwnerName,
            LandOwnerNameEnglish = property.LandOwnerNameEnglish,
            BuilderName = property.BuilderName,
            BuilderNameEnglish = property.BuilderNameEnglish,
            BuilderMobileNo = property.BuilderMobileNo,
            Owner = property.OwnerNameEnglish,
            HolderRegional = property.OwnerName,
            OccupierName = property.OccupierNameEnglish,
            OccupierRegional = property.OccupierName,
            OwnerCategory = property.OwnerCategory,
            SocietyBuildingPhotoGuid = property.SocietyBuildingPhotoGuid,
            Wings = wings
        };


        var taxZone = property.TaxZoneNo is null
            ? null
            : string.IsNullOrWhiteSpace(property.TaxZoneRemark) ? property.TaxZoneNo : $"{property.TaxZoneNo} - {property.TaxZoneRemark}";

        var info = new PropertyInfoDto
        {
            Division = property.DivisionName,
            MoujaName = property.MoujaName,
            SurveyNo = property.Csn,
            PlotNo = property.PlotNo,
            TaxZone = taxZone,
            MobileNo = property.MobileNo,
            AlternateMobileNo = property.AlternateMobileNo,
            AadharNo = property.AadharCardNo,
            EmailId = property.EmailId,
            Address = property.Address,
            Pincode = property.PinCode,
            PlotAreaFt = plotFt,
            PlotAreaMtr = plotMtr,
            CarpetAreaFt = carpetFt,
            CarpetAreaMtr = carpetMtr,
            BuiltUpAreaFt = builtUpFt,
            BuiltUpAreaMtr = builtUpMtr,
        };

        var additionalRevenue = _calculator.ComputeAdditionalRevenue(currentTax, retroTax, oldCurrentTax, pendingCurrent);

        return new PropertyTopSectionDto
        {
            PropertyOverview = overview,
            PropertyInfo = info,
            AdditionalRevenue = additionalRevenue
        };
    }

    public async Task<TopSectionUpdateOutcome> UpdateTopSectionAsync(
        int propertyId,
        UpdateApartmentQcTopSectionDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (dto is null || !dto.HasAnyField())
        {
            return TopSectionUpdateOutcome.NoFieldsProvided;
        }

        return await _repository.UpdateTopSectionAsync(propertyId, dto, updatedBy, cancellationToken);
    }

    public async Task<TopSectionUpdateOutcome> UpdateWingDetailsAsync(
        int wingDetailId,
        UpdateApartmentQcWingDetailsDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (dto is null || !dto.HasAnyField())
        {
            return TopSectionUpdateOutcome.NoFieldsProvided;
        }

        return await _repository.UpdateWingDetailsAsync(wingDetailId, dto, updatedBy, cancellationToken);
    }
}

