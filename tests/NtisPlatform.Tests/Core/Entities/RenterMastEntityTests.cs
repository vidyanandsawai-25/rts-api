using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Unit tests for RenterMastEntity
/// </summary>
public class RenterMastEntityTests
{
    [Fact]
    public void RenterMastEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new RenterMastEntity
        {
            Id = 1,
            PropertyDetailsId = 100,
            RentMonthly = 25000.0,
            FinalYearlyRent = 300000.0,
            FinancialYear = "2023",
            DurationFrom = now.AddYears(-1),
            DurationTo = now.AddYears(1),
            TaxLiability = "Liable",
            NonCalculateRentMonthly = 5000.0,
            RenterNameEnglish = "John Doe",
            RenterName = "जॉन डो",
            AgreementDate = now.AddMonths(-12),
            AgreementFromDate = now.AddYears(-1),
            AgreementToDate = now.AddYears(1),
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.PropertyDetailsId);
        Assert.Equal(25000.0, entity.RentMonthly);
        Assert.Equal(300000.0, entity.FinalYearlyRent);
        Assert.Equal("2023", entity.FinancialYear);
        Assert.Equal(now.AddYears(-1), entity.DurationFrom);
        Assert.Equal(now.AddYears(1), entity.DurationTo);
        Assert.Equal("Liable", entity.TaxLiability);
        Assert.Equal(5000.0, entity.NonCalculateRentMonthly);
        Assert.Equal("John Doe", entity.RenterNameEnglish);
        Assert.Equal("जॉन डो", entity.RenterName);
        Assert.Equal(now.AddMonths(-12), entity.AgreementDate);
        Assert.Equal(now.AddYears(-1), entity.AgreementFromDate);
        Assert.Equal(now.AddYears(1), entity.AgreementToDate);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void RenterMastEntity_OptionalProperties_CanBeNull()
    {
        var entity = new RenterMastEntity
        {
            PropertyDetailsId = 100,
            IsActive = true
        };

        Assert.Null(entity.RentMonthly);
        Assert.Null(entity.FinalYearlyRent);
        Assert.Null(entity.FinancialYear);
        Assert.Null(entity.DurationFrom);
        Assert.Null(entity.DurationTo);
        Assert.Null(entity.TaxLiability);
        Assert.Null(entity.NonCalculateRentMonthly);
        Assert.Null(entity.RenterNameEnglish);
        Assert.Null(entity.RenterName);
        Assert.Null(entity.AgreementDate);
        Assert.Null(entity.AgreementFromDate);
        Assert.Null(entity.AgreementToDate);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void RenterMastEntity_MarkedForDeletion_DefaultsToFalse()
    {
        var entity = new RenterMastEntity();
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void RenterMastEntity_NavigationProperty_PropertyDetails_CanBeSet()
    {
        var renterEntity = new RenterMastEntity
        {
            Id = 1,
            PropertyDetailsId = 100
        };

        var propertyDetailsEntity = new PropertyDetailsEntity
        {
            Id = 100,
            PropertyId = 549357,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4
        };

        renterEntity.PropertyDetails = propertyDetailsEntity;

        Assert.NotNull(renterEntity.PropertyDetails);
        Assert.Equal(100, renterEntity.PropertyDetails.Id);
        Assert.Equal(549357, renterEntity.PropertyDetails.PropertyId);
    }

    [Fact]
    public void RenterMastEntity_RentalAmounts_WorksCorrectly()
    {
        var entity = new RenterMastEntity
        {
            RentMonthly = 30000.0,
            FinalYearlyRent = 360000.0,
            NonCalculateRentMonthly = 5000.0
        };

        Assert.Equal(30000.0, entity.RentMonthly);
        Assert.Equal(360000.0, entity.FinalYearlyRent);
        Assert.Equal(5000.0, entity.NonCalculateRentMonthly);
    }

    [Fact]
    public void RenterMastEntity_DurationDates_WorksCorrectly()
    {
        var startDate = new DateTime(2023, 4, 1);
        var endDate = new DateTime(2024, 3, 31);

        var entity = new RenterMastEntity
        {
            DurationFrom = startDate,
            DurationTo = endDate,
            FinancialYear = "2023"
        };

        Assert.Equal(startDate, entity.DurationFrom);
        Assert.Equal(endDate, entity.DurationTo);
        Assert.Equal("2023", entity.FinancialYear);
    }
}
