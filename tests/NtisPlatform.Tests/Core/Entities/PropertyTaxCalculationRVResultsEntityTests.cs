using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

public class PropertyTaxCalculationRVResultsEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new PropertyTaxCalculationRVResultsEntity
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
            TaxId = 2,
            TaxPercentage = 5.5m,
            TaxAmount = 62.7m,
            REducationTax = 1.0m,
            CEducationTax = 1.5m,
            REducationTaxPercentage = 0.1m,
            CEducationTaxPercentage = 0.15m,
            REmploymentTax = 2.0m,
            CEmploymentTax = 2.5m,
            REmploymentTaxPercentage = 0.2m,
            CEmploymentTaxPercentage = 0.25m,
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
        Assert.Equal(2, entity.TaxId);
        Assert.Equal(5.5m, entity.TaxPercentage);
        Assert.Equal(62.7m, entity.TaxAmount);
        Assert.Equal(1.0m, entity.REducationTax);
        Assert.Equal(1.5m, entity.CEducationTax);
        Assert.Equal(0.1m, entity.REducationTaxPercentage);
        Assert.Equal(0.15m, entity.CEducationTaxPercentage);
        Assert.Equal(2.0m, entity.REmploymentTax);
        Assert.Equal(2.5m, entity.CEmploymentTax);
        Assert.Equal(0.2m, entity.REmploymentTaxPercentage);
        Assert.Equal(0.25m, entity.CEmploymentTaxPercentage);
        Assert.Equal(100.0d, entity.TotalAreaSqMtr);
        Assert.Equal(60.0d, entity.RAreaSqMtr);
        Assert.Equal(40.0d, entity.CAreaSqlMtr);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }
}
