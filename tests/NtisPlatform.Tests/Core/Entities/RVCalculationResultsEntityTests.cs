using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

public class RVCalculationResultsEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new RVCalculationResultsEntity
        {
            Id = 1,
            PropertyId = 9,
            PropertyDetailsId = 99,
            MonthlyRate = 100.5d,
            YearlyRate = 1206.0d,
            YearlyRent = 1200.0d,
            Depreciation = 50.0m,


            AnnualRentalValue = 1150.0d,
            Maintenance = 10.0m,
            RateableValue = 1140.0m,
            REducationTax = 1.0m,
            CEducationTax = 1.5m,
            CEmploymentTax = 2.5m,
            TotalAreaSqMtr = 100.0d,
            RAreaSqMtr = 60.0d,
            CAreaSqlMtr = 40.0d,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(9, entity.PropertyId);
        Assert.Equal(99, entity.PropertyDetailsId);
        Assert.Equal(100.5d, entity.MonthlyRate);
        Assert.Equal(1206.0d, entity.YearlyRate);
        Assert.Equal(1200.0d, entity.YearlyRent);
        Assert.Equal(50.0m, entity.Depreciation);
        Assert.Equal(1150.0d, entity.AnnualRentalValue);
        Assert.Equal(10.0m, entity.Maintenance);
        Assert.Equal(1140.0m, entity.RateableValue);
        Assert.Equal(1.0m, entity.REducationTax);
        Assert.Equal(1.5m, entity.CEducationTax);
        Assert.Equal(2.5m, entity.CEmploymentTax);
        Assert.Equal(100.0d, entity.TotalAreaSqMtr);
        Assert.Equal(60.0d, entity.RAreaSqMtr);
        Assert.Equal(40.0d, entity.CAreaSqlMtr);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.NotNull(entity.TaxDetails);
        Assert.Empty(entity.TaxDetails);
    }

    [Fact]
    public void TaxDetails_NavigationProperty_Initialized()
    {
        var entity = new RVCalculationResultsEntity();
        Assert.NotNull(entity.TaxDetails);
        Assert.Empty(entity.TaxDetails);
    }
}

