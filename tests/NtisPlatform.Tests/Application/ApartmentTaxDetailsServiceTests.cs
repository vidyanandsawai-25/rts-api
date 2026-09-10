using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class ApartmentTaxDetailsServiceTests
{
    private readonly Mock<IApartmentTaxDetailsRepository> _repository = new();
    private readonly ApartmentTaxDetailsService _service;

    public ApartmentTaxDetailsServiceTests()
    {
        _service = new ApartmentTaxDetailsService(_repository.Object);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_NoPropertyMatch_ReturnsNull()
    {
        _repository.Setup(r => r.GetTaxDetailsAsync(It.IsAny<ApartmentTaxDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApartmentTaxDetailsRawData?)null);

        var result = await _service.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "999", TaxType = ApartmentTaxType.RV });

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_MapsRawDataToDto()
    {
        var query = new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", WingMasterId = 1, TaxType = ApartmentTaxType.Dual };
        var raw = new ApartmentTaxDetailsRawData
        {
            PropertyCount = 3,
            WingMasterId = 1,
            WingName = "A Wing",
            WingNo = "A",
            SocietyName = "Test Society",
            CurrentTaxes = new List<CurrentTaxByTypeDto>
            {
                new() { TaxType = "RV", TaxHeads = new List<TaxHeadAmountDto> { new() { TaxId = 1, TaxName = "General Tax", TaxAmount = 250m } } },
                new() { TaxType = "CV", TaxHeads = new List<TaxHeadAmountDto> { new() { TaxId = 1, TaxName = "General Tax", TaxAmount = 300m } } }
            },
            Arrears = new List<TaxHeadAmountDto> { new() { TaxId = 1, TaxName = "General Tax", TaxAmount = 500m } }
        };
        _repository.Setup(r => r.GetTaxDetailsAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(raw);

        var result = await _service.GetTaxDetailsAsync(query);

        Assert.NotNull(result);
        Assert.Equal(96, result!.WardId);
        Assert.Equal("20", result.PropertyNo);
        Assert.Equal(1, result.WingMasterId);
        Assert.Equal("A Wing", result.WingName);
        Assert.Equal("A", result.WingNo);
        Assert.Equal("Test Society", result.SocietyName);
        Assert.Equal(3, result.PropertyCount);
        Assert.Equal(2, result.CurrentTaxes.Count);
        var arrears = Assert.Single(result.Arrears);
        Assert.Equal(500m, arrears.TaxAmount);
    }
}
