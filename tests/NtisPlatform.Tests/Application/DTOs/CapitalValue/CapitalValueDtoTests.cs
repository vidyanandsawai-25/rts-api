using NtisPlatform.Application.DTOs.CapitalValue;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.CapitalValue;

public class CapitalValueDtoTests
{
    [Fact]
    public void CapitalValueDto_GetSet_Works()
    {
        var dto = new CapitalValueDto
        {
            PropertyId = 1,
            PropertyDetailsId = 10,
            CapitalValue = 1000,
            BaseValue = 500,
            FloorFactor = 1.2,
            SDRR = 100,
            UseFactor = 1.0,
            NTBFactor = 1.0,
            AgeFactor = 1.0
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(10, dto.PropertyDetailsId);
        Assert.Equal(1000, dto.CapitalValue);
        Assert.Equal(500, dto.BaseValue);
    }

    [Fact]
    public void CapitalValueDto_TaxesCollection_Works()
    {
        var dto = new CapitalValueDto
        {
            Taxes = new List<TaxHeadDto>
            {
                new TaxHeadDto { TaxId = 1, TaxName = "Property Tax", Percentage = 15, Amount = 50000 },
                new TaxHeadDto { TaxId = 2, TaxName = "Education Tax", Percentage = 5, Amount = 16666 }
            }
        };

        Assert.Equal(2, dto.Taxes.Count);
        Assert.Equal(50000, dto.Taxes[0].Amount);
    }
}

public class CreateCapitalValueDtoTests
{
    [Fact]
    public void CreateDto_GetSet_Works()
    {
        var dto = new CreateCapitalValueDto
        {
            PropertyId = 1,
            PropertyDetailsId = 10,
            PolicyCode = "TEST",
            CreatedBy = 100
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(10, dto.PropertyDetailsId);
        Assert.Equal("TEST", dto.PolicyCode);
        Assert.Equal(100, dto.CreatedBy);
    }
}
