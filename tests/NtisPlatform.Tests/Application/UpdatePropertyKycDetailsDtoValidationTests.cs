using System.ComponentModel.DataAnnotations;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive validation tests for UpdatePropertyKycDetailsDto
/// Covers all validation attributes and edge cases
/// </summary>
public class UpdatePropertyKycDetailsDtoValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_ValidData_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            OwnerTypeId = 1,
            AdharCardNo = "321131311616",
            OwnerTitle = "Mr",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr",
            OwnerNameEnglish = "John English",
            OccupierTitle = "Ms",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Ms",
            OccupierNameEnglish = "Jane English",
            Address = "123 Main St",
            Location = "Downtown",
            AddressEnglish = "123 Main Street",
            LocationEnglish = "Downtown Area",
            FlatOrShopName = "Shop 101",
            FlatOrShopNameEnglish = "Shop English",
            FlatOrShopNo = "101",
            FlatOrShopNoEnglish = "101",
            MobileNo = "9921759522",
            EmailId = "test@example.com"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_AllPropertiesNull_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto();

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_DefaultConstructor_AllNullablesAreNull()
    {
        var dto = new UpdatePropertyKycDetailsDto();

        Assert.Null(dto.OwnerTypeId);
        Assert.Null(dto.AdharCardNo);
        Assert.Null(dto.OwnerTitle);
        Assert.Null(dto.OwnerName);
        Assert.Null(dto.OwnerTitleEnglish);
        Assert.Null(dto.OwnerNameEnglish);
        Assert.Null(dto.OccupierTitle);
        Assert.Null(dto.OccupierName);
        Assert.Null(dto.OccupierTitleEnglish);
        Assert.Null(dto.OccupierNameEnglish);
        Assert.Null(dto.Address);
        Assert.Null(dto.Location);
        Assert.Null(dto.AddressEnglish);
        Assert.Null(dto.LocationEnglish);
        Assert.Null(dto.FlatOrShopName);
        Assert.Null(dto.FlatOrShopNameEnglish);
        Assert.Null(dto.FlatOrShopNo);
        Assert.Null(dto.FlatOrShopNoEnglish);
        Assert.Null(dto.MobileNo);
        Assert.Null(dto.EmailId);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_OnlyOwnerFields_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            OwnerTitle = "Mr",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr",
            OwnerNameEnglish = "John English"
        };

        var results = Validate(dto);
        Assert.Empty(results);
        
        Assert.Equal("Mr", dto.OwnerTitle);
        Assert.Equal("John Doe", dto.OwnerName);
        Assert.Null(dto.OccupierName);
        Assert.Null(dto.Address);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_OnlyOccupierFields_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            OccupierTitle = "Ms",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Ms",
            OccupierNameEnglish = "Jane English"
        };

        var results = Validate(dto);
        Assert.Empty(results);
        
        Assert.Equal("Ms", dto.OccupierTitle);
        Assert.Equal("Jane Doe", dto.OccupierName);
        Assert.Null(dto.OwnerName);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_OnlyAddressFields_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            Address = "123 Main St",
            Location = "Downtown",
            AddressEnglish = "123 Main Street",
            LocationEnglish = "Downtown Area"
        };

        var results = Validate(dto);
        Assert.Empty(results);
        
        Assert.Equal("123 Main St", dto.Address);
        Assert.Equal("Downtown", dto.Location);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_OnlyContactFields_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            MobileNo = "9921759522",
            EmailId = "test@example.com"
        };

        var results = Validate(dto);
        Assert.Empty(results);
        
        Assert.Equal("9921759522", dto.MobileNo);
        Assert.Equal("test@example.com", dto.EmailId);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_OnlyFlatShopFields_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            FlatOrShopName = "Shop 101",
            FlatOrShopNameEnglish = "Shop English",
            FlatOrShopNo = "101",
            FlatOrShopNoEnglish = "101"
        };

        var results = Validate(dto);
        Assert.Empty(results);
        
        Assert.Equal("Shop 101", dto.FlatOrShopName);
        Assert.Equal("101", dto.FlatOrShopNo);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_OnlyOwnerTypeAndAdhar_PassesValidation()
    {
        var dto = new UpdatePropertyKycDetailsDto
        {
            OwnerTypeId = 1,
            AdharCardNo = "123456789012"
        };

        var results = Validate(dto);
        Assert.Empty(results);
        
        Assert.Equal(1, dto.OwnerTypeId);
        Assert.Equal("123456789012", dto.AdharCardNo);
    }

    [Fact]
    public void UpdatePropertyKycDetailsDto_SetAndGetAllProperties_WorksCorrectly()
    {
        var dto = new UpdatePropertyKycDetailsDto();
        
        dto.OwnerTypeId = 1;
        dto.AdharCardNo = "321131311616";
        dto.OwnerTitle = "Mr";
        dto.OwnerName = "Owner";
        dto.OwnerTitleEnglish = "Mr";
        dto.OwnerNameEnglish = "OwnerEng";
        dto.OccupierTitle = "Ms";
        dto.OccupierName = "Occupier";
        dto.OccupierTitleEnglish = "Ms";
        dto.OccupierNameEnglish = "OccupierEng";
        dto.Address = "Addr";
        dto.Location = "Loc";
        dto.AddressEnglish = "AddrEng";
        dto.LocationEnglish = "LocEng";
        dto.FlatOrShopName = "Flat";
        dto.FlatOrShopNameEnglish = "FlatEng";
        dto.FlatOrShopNo = "101";
        dto.FlatOrShopNoEnglish = "101";
        dto.MobileNo = "9921759522";
        dto.EmailId = "test@test.com";

        Assert.Equal(1, dto.OwnerTypeId);
        Assert.Equal("321131311616", dto.AdharCardNo);
        Assert.Equal("Mr", dto.OwnerTitle);
        Assert.Equal("Owner", dto.OwnerName);
        Assert.Equal("Mr", dto.OwnerTitleEnglish);
        Assert.Equal("OwnerEng", dto.OwnerNameEnglish);
        Assert.Equal("Ms", dto.OccupierTitle);
        Assert.Equal("Occupier", dto.OccupierName);
        Assert.Equal("Ms", dto.OccupierTitleEnglish);
        Assert.Equal("OccupierEng", dto.OccupierNameEnglish);
        Assert.Equal("Addr", dto.Address);
        Assert.Equal("Loc", dto.Location);
        Assert.Equal("AddrEng", dto.AddressEnglish);
        Assert.Equal("LocEng", dto.LocationEnglish);
        Assert.Equal("Flat", dto.FlatOrShopName);
        Assert.Equal("FlatEng", dto.FlatOrShopNameEnglish);
        Assert.Equal("101", dto.FlatOrShopNo);
        Assert.Equal("101", dto.FlatOrShopNoEnglish);
        Assert.Equal("9921759522", dto.MobileNo);
        Assert.Equal("test@test.com", dto.EmailId);
    }
}
