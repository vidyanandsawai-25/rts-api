using NtisPlatform.Application.Services;
using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Tests.Application;

public class ServiceManagementServiceTests
{
    private readonly ServiceManagementService _service;

    public ServiceManagementServiceTests()
    {
        _service = new ServiceManagementService();
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnAllServices()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.Count);
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnServicesWithCorrectStructure()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        foreach (var service in result)
        {
            Assert.True(service.Id > 0);
            Assert.NotEmpty(service.Link);
            Assert.NotEmpty(service.Icon);
            Assert.NotEmpty(service.Title);
            Assert.NotEmpty(service.Subtext);
            Assert.NotNull(service.Stats);
            Assert.Equal(3, service.Stats.Count);
        }
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnPropertyTaxAsFirstService()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        var firstService = result.First();
        Assert.Equal(1, firstService.Id);
        Assert.Equal("/propertySearch", firstService.Link);
        Assert.Equal("home", firstService.Icon);
        Assert.Equal("Property Tax", firstService.Title);
        Assert.Contains("property taxes online", firstService.Subtext);
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnServicesWithValidStats()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        var propertyTax = result.First(s => s.Id == 1);
        Assert.Equal(3, propertyTax.Stats.Count);
        
        var totalStat = propertyTax.Stats.First(s => s.Label == "Total");
        var paidStat = propertyTax.Stats.First(s => s.Label == "Paid");
        var remainingStat = propertyTax.Stats.First(s => s.Label == "Remaining");
        
        Assert.Equal("12,345", totalStat.Value);
        Assert.Equal("9,876", paidStat.Value);
        Assert.Equal("2,469", remainingStat.Value);
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnAllExpectedServiceIds()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        var expectedIds = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var actualIds = result.Select(s => s.Id).ToArray();
        
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnServicesWithUniqueIds()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        var uniqueIds = result.Select(s => s.Id).Distinct().Count();
        Assert.Equal(result.Count, uniqueIds);
    }

    [Fact]
    public async Task GetServicesAsync_ShouldReturnServicesWithValidIconIdentifiers()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        var expectedIcons = new[] 
        { 
            "home", "droplet", "shopping-bag", "file-text", 
            "trash-2", "building-2", "megaphone", "clock", "landmark" 
        };
        
        var actualIcons = result.Select(s => s.Icon).ToArray();
        Assert.Equal(expectedIcons, actualIcons);
    }

    [Fact]
    public async Task GetServicesAsync_EachServiceShouldHaveTotalPaidAndRemainingStats()
    {
        // Act
        var result = await _service.GetServicesAsync();

        // Assert
        foreach (var service in result)
        {
            var statLabels = service.Stats.Select(s => s.Label).ToArray();
            Assert.Contains("Total", statLabels);
            Assert.Contains("Paid", statLabels);
            Assert.Contains("Remaining", statLabels);
        }
    }
}
