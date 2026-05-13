using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Unit tests for RenterDetailEntity
/// </summary>
public class RenterDetailEntityTests
{
    [Fact]
    public void RenterDetailEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new RenterDetailEntity
        {
            Id = 1,
            PropertyDetailsId = 100,
            AgreementId = "AGR-2023-001",
            IncrementFrequency = "Yearly",
            IncrementType = "Percentage",
            IncrementValue = 10.0,
            IncrementMethod = "Compound",
            DurationFrom = now.AddYears(-1),
            DurationTo = now.AddYears(1),
            RentAmount = 300000.0,
            RentMonthly = 25000.0,
            Increment = 2500.0,
            IncrementStatus = true,
            CustomFromDate = now.AddMonths(-6),
            CustomToDate = now.AddMonths(6),
            CustomIncrementType = "Fixed",
            CustomIncrementValue = 5000.0,
            CustomMethod = "Simple",
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
        Assert.Equal("AGR-2023-001", entity.AgreementId);
        Assert.Equal("Yearly", entity.IncrementFrequency);
        Assert.Equal("Percentage", entity.IncrementType);
        Assert.Equal(10.0, entity.IncrementValue);
        Assert.Equal("Compound", entity.IncrementMethod);
        Assert.Equal(now.AddYears(-1), entity.DurationFrom);
        Assert.Equal(now.AddYears(1), entity.DurationTo);
        Assert.Equal(300000.0, entity.RentAmount);
        Assert.Equal(25000.0, entity.RentMonthly);
        Assert.Equal(2500.0, entity.Increment);
        Assert.True(entity.IncrementStatus.GetValueOrDefault());
        Assert.Equal(now.AddMonths(-6), entity.CustomFromDate);
        Assert.Equal(now.AddMonths(6), entity.CustomToDate);
        Assert.Equal("Fixed", entity.CustomIncrementType);
        Assert.Equal(5000.0, entity.CustomIncrementValue);
        Assert.Equal("Simple", entity.CustomMethod);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void RenterDetailEntity_OptionalProperties_CanBeNull()
    {
        var entity = new RenterDetailEntity
        {
            PropertyDetailsId = 100,
            IsActive = true
        };

        Assert.Null(entity.AgreementId);
        Assert.Null(entity.IncrementFrequency);
        Assert.Null(entity.IncrementType);
        Assert.Null(entity.IncrementValue);
        Assert.Null(entity.IncrementMethod);
        Assert.Null(entity.DurationFrom);
        Assert.Null(entity.DurationTo);
        Assert.Null(entity.RentAmount);
        Assert.Null(entity.RentMonthly);
        Assert.Null(entity.Increment);
        Assert.Null(entity.IncrementStatus);
        Assert.Null(entity.CustomFromDate);
        Assert.Null(entity.CustomToDate);
        Assert.Null(entity.CustomIncrementType);
        Assert.Null(entity.CustomIncrementValue);
        Assert.Null(entity.CustomMethod);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void RenterDetailEntity_MarkedForDeletion_DefaultsToFalse()
    {
        var entity = new RenterDetailEntity();
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void RenterDetailEntity_NavigationProperty_PropertyDetails_CanBeSet()
    {
        var renterDetailEntity = new RenterDetailEntity
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

        renterDetailEntity.PropertyDetails = propertyDetailsEntity;

        Assert.NotNull(renterDetailEntity.PropertyDetails);
        Assert.Equal(100, renterDetailEntity.PropertyDetails.Id);
        Assert.Equal(549357, renterDetailEntity.PropertyDetails.PropertyId);
    }

    [Fact]
    public void RenterDetailEntity_IncrementSettings_WorksCorrectly()
    {
        var entity = new RenterDetailEntity
        {
            IncrementFrequency = "Yearly",
            IncrementType = "Percentage",
            IncrementValue = 10.0,
            IncrementMethod = "Compound",
            IncrementStatus = true
        };

        Assert.Equal("Yearly", entity.IncrementFrequency);
        Assert.Equal("Percentage", entity.IncrementType);
        Assert.Equal(10.0, entity.IncrementValue);
        Assert.Equal("Compound", entity.IncrementMethod);
        Assert.Equal(true, entity.IncrementStatus);
    }

    [Fact]
    public void RenterDetailEntity_CustomIncrementSettings_WorksCorrectly()
    {
        var customFromDate = new DateTime(2023, 1, 1);
        var customToDate = new DateTime(2023, 12, 31);

        var entity = new RenterDetailEntity
        {
            CustomFromDate = customFromDate,
            CustomToDate = customToDate,
            CustomIncrementType = "Fixed",
            CustomIncrementValue = 5000.0,
            CustomMethod = "Simple"
        };

        Assert.Equal(customFromDate, entity.CustomFromDate);
        Assert.Equal(customToDate, entity.CustomToDate);
        Assert.Equal("Fixed", entity.CustomIncrementType);
        Assert.Equal(5000.0, entity.CustomIncrementValue);
        Assert.Equal("Simple", entity.CustomMethod);
    }

    [Fact]
    public void RenterDetailEntity_RentAmounts_WorksCorrectly()
    {
        var entity = new RenterDetailEntity
        {
            RentAmount = 360000.0,
            RentMonthly = 30000.0,
            Increment = 3000.0
        };

        Assert.Equal(360000.0, entity.RentAmount);
        Assert.Equal(30000.0, entity.RentMonthly);
        Assert.Equal(3000.0, entity.Increment);
    }

    [Fact]
    public void RenterDetailEntity_DurationDates_WorksCorrectly()
    {
        var startDate = new DateTime(2023, 4, 1);
        var endDate = new DateTime(2024, 3, 31);

        var entity = new RenterDetailEntity
        {
            DurationFrom = startDate,
            DurationTo = endDate
        };

        Assert.Equal(startDate, entity.DurationFrom);
        Assert.Equal(endDate, entity.DurationTo);
    }
}
