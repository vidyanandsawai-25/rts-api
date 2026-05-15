using NtisPlatform.Application.DTOs.Property;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Property;

public class CreatePropertyDtoTests
{
    [Fact]
    public void PropertyNo_SetsAndGets_CorrectValue()
    {
        var dto = new CreatePropertyDto { PropertyNo = "  P123  " };
        Assert.Equal("P123", dto.PropertyNo);
    }

    [Fact]
    public void PropertyNo_WithNull_ReturnsNull()
    {
        var dto = new CreatePropertyDto { PropertyNo = null };
        Assert.Null(dto.PropertyNo);
    }

    [Fact]
    public void PropertyNo_WithWhitespace_ReturnsNull()
    {
        var dto = new CreatePropertyDto { PropertyNo = "   " };
        Assert.Null(dto.PropertyNo);
    }

    [Fact]
    public void PropertyNo_ExceedsMaxLength_ValidationFails()
    {
        var dto = new CreatePropertyDto { PropertyNo = "12345678901", TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.PropertyNo) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.PropertyNo, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void PartitionNo_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { PartitionNo = "  PART1  " };
        Assert.Equal("PART1", dto.PartitionNo);
    }

    [Fact]
    public void PartitionNo_WithNull_ReturnsNull()
    {
        var dto = new CreatePropertyDto { PartitionNo = null };
        Assert.Null(dto.PartitionNo);
    }

    [Fact]
    public void PartitionNo_WithWhitespace_ReturnsNull()
    {
        var dto = new CreatePropertyDto { PartitionNo = "   " };
        Assert.Null(dto.PartitionNo);
    }

    [Fact]
    public void UPICId_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { UPICId = "  UPIC123  " };
        Assert.Equal("UPIC123", dto.UPICId);
    }

    [Fact]
    public void UPICId_WithValidCharacters_Validates()
    {
        var dto = new CreatePropertyDto { UPICId = "ABC-123_XYZ", TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.UPICId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.UPICId, context, results);
        Assert.True(isValid);
    }

    [Fact]
    public void UPICId_WithInvalidCharacters_ValidationFails()
    {
        var dto = new CreatePropertyDto { UPICId = "ABC@123", TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.UPICId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.UPICId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void CSN_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { CSN = "  CSN123  " };
        Assert.Equal("CSN123", dto.CSN);
    }

    [Fact]
    public void SubZoneNo_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { SubZoneNo = "  ZONE1  " };
        Assert.Equal("ZONE1", dto.SubZoneNo);
    }

    [Fact]
    public void PlotNo_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { PlotNo = "  PLOT123  " };
        Assert.Equal("PLOT123", dto.PlotNo);
    }

    [Fact]
    public void PartType_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { PartType = "  TypeA  " };
        Assert.Equal("TypeA", dto.PartType);
    }

    [Fact]
    public void OwnerTitle_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OwnerTitle = "  Mr.  " };
        Assert.Equal("Mr.", dto.OwnerTitle);
    }

    [Fact]
    public void OwnerName_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OwnerName = "  John Doe  " };
        Assert.Equal("John Doe", dto.OwnerName);
    }

    [Fact]
    public void OwnerTitleEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OwnerTitleEnglish = "  Mr.  " };
        Assert.Equal("Mr.", dto.OwnerTitleEnglish);
    }

    [Fact]
    public void OwnerNameEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OwnerNameEnglish = "  John Doe  " };
        Assert.Equal("John Doe", dto.OwnerNameEnglish);
    }

    [Fact]
    public void OccupierTitle_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OccupierTitle = "  Mrs.  " };
        Assert.Equal("Mrs.", dto.OccupierTitle);
    }

    [Fact]
    public void OccupierName_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OccupierName = "  Jane Smith  " };
        Assert.Equal("Jane Smith", dto.OccupierName);
    }

    [Fact]
    public void OccupierTitleEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OccupierTitleEnglish = "  Mrs.  " };
        Assert.Equal("Mrs.", dto.OccupierTitleEnglish);
    }

    [Fact]
    public void OccupierNameEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { OccupierNameEnglish = "  Jane Smith  " };
        Assert.Equal("Jane Smith", dto.OccupierNameEnglish);
    }

    [Fact]
    public void FlatOrShopNo_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { FlatOrShopNo = "  101  " };
        Assert.Equal("101", dto.FlatOrShopNo);
    }

    [Fact]
    public void FlatOrShopName_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { FlatOrShopName = "  Shop Name  " };
        Assert.Equal("Shop Name", dto.FlatOrShopName);
    }

    [Fact]
    public void FlatOrShopNoEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { FlatOrShopNoEnglish = "  101  " };
        Assert.Equal("101", dto.FlatOrShopNoEnglish);
    }

    [Fact]
    public void FlatOrShopNameEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { FlatOrShopNameEnglish = "  Shop Name  " };
        Assert.Equal("Shop Name", dto.FlatOrShopNameEnglish);
    }

    [Fact]
    public void Address_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { Address = "  123 Main St  " };
        Assert.Equal("123 Main St", dto.Address);
    }

    [Fact]
    public void Location_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { Location = "  Downtown  " };
        Assert.Equal("Downtown", dto.Location);
    }

    [Fact]
    public void AddressEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { AddressEnglish = "  123 Main St  " };
        Assert.Equal("123 Main St", dto.AddressEnglish);
    }

    [Fact]
    public void LocationEnglish_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { LocationEnglish = "  Downtown  " };
        Assert.Equal("Downtown", dto.LocationEnglish);
    }

    [Fact]
    public void MobileNo_TrimsWhitespace()
    {
        var dto = new CreatePropertyDto { MobileNo = "  9876543210  " };
        Assert.Equal("9876543210", dto.MobileNo);
    }

    [Fact]
    public void MobileNo_WithInvalidCharacters_ValidationFails()
    {
        var dto = new CreatePropertyDto { MobileNo = "ABC123", TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.MobileNo) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.MobileNo, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void EmailId_TrimsAndConvertsToLowercase()
    {
        var dto = new CreatePropertyDto { EmailId = "  TEST@EMAIL.COM  " };
        Assert.Equal("test@email.com", dto.EmailId);
    }

    [Fact]
    public void EmailId_WithValidFormat_Validates()
    {
        var dto = new CreatePropertyDto { EmailId = "test@example.com", TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.EmailId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.EmailId, context, results);
        Assert.True(isValid);
    }

    [Fact]
    public void EmailId_WithInvalidFormat_ValidationFails()
    {
        var dto = new CreatePropertyDto { EmailId = "invalid-email", TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.EmailId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.EmailId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void TaxZoneId_Required_ValidationFails()
    {
        var dto = new CreatePropertyDto { TaxZoneId = 0, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.TaxZoneId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.TaxZoneId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void WardId_Required_ValidationFails()
    {
        var dto = new CreatePropertyDto { TaxZoneId = 1, WardId = 0 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.WardId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.WardId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void PropertyTypeId_WithZero_ValidationFails()
    {
        var dto = new CreatePropertyDto { PropertyTypeId = 0, TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.PropertyTypeId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.PropertyTypeId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void CategoryId_WithZero_ValidationFails()
    {
        var dto = new CreatePropertyDto { CategoryId = 0, TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.CategoryId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.CategoryId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void SocietyDetailId_WithZero_ValidationFails()
    {
        var dto = new CreatePropertyDto { SocietyDetailId = 0, TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.SocietyDetailId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.SocietyDetailId, context, results);
        Assert.False(isValid);
    }

    [Fact]
    public void OpenPlot_DefaultsToNull()
    {
        var dto = new CreatePropertyDto();
        Assert.Null(dto.OpenPlot);
    }

    [Fact]
    public void OpenPlot_CanBeSetToTrue()
    {
        var dto = new CreatePropertyDto { OpenPlot = true };
        Assert.True(dto.OpenPlot);
    }

    [Fact]
    public void OpenPlot_CanBeSetToFalse()
    {
        var dto = new CreatePropertyDto { OpenPlot = false };
        Assert.False(dto.OpenPlot);
    }

    [Fact]
    public void MarkedForDeletion_DefaultsToFalse()
    {
        var dto = new CreatePropertyDto();
        Assert.False(dto.MarkedForDeletion);
    }

    [Fact]
    public void MarkedForDeletion_CanBeSetToTrue()
    {
        var dto = new CreatePropertyDto { MarkedForDeletion = true };
        Assert.True(dto.MarkedForDeletion);
    }

    [Fact]
    public void AllStringProperties_WithEmptyString_ReturnsNull()
    {
        var dto = new CreatePropertyDto
        {
            PropertyNo = "",
            PartitionNo = "",
            UPICId = "",
            CSN = "",
            SubZoneNo = "",
            PlotNo = "",
            PartType = "",
            OwnerTitle = "",
            OwnerName = "",
            OwnerTitleEnglish = "",
            OwnerNameEnglish = "",
            OccupierTitle = "",
            OccupierName = "",
            OccupierTitleEnglish = "",
            OccupierNameEnglish = "",
            FlatOrShopNo = "",
            FlatOrShopName = "",
            FlatOrShopNoEnglish = "",
            FlatOrShopNameEnglish = "",
            Address = "",
            Location = "",
            AddressEnglish = "",
            LocationEnglish = "",
            MobileNo = "",
            EmailId = ""
        };

        Assert.Null(dto.PropertyNo);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.UPICId);
        Assert.Null(dto.CSN);
        Assert.Null(dto.SubZoneNo);
        Assert.Null(dto.PlotNo);
        Assert.Null(dto.PartType);
        Assert.Null(dto.OwnerTitle);
        Assert.Null(dto.OwnerName);
        Assert.Null(dto.OwnerTitleEnglish);
        Assert.Null(dto.OwnerNameEnglish);
        Assert.Null(dto.OccupierTitle);
        Assert.Null(dto.OccupierName);
        Assert.Null(dto.OccupierTitleEnglish);
        Assert.Null(dto.OccupierNameEnglish);
        Assert.Null(dto.FlatOrShopNo);
        Assert.Null(dto.FlatOrShopName);
        Assert.Null(dto.FlatOrShopNoEnglish);
        Assert.Null(dto.FlatOrShopNameEnglish);
        Assert.Null(dto.Address);
        Assert.Null(dto.Location);
        Assert.Null(dto.AddressEnglish);
        Assert.Null(dto.LocationEnglish);
        Assert.Null(dto.MobileNo);
        Assert.Null(dto.EmailId);
    }

    [Fact]
    public void PropertyTypeId_WithValidValue_Validates()
    {
        var dto = new CreatePropertyDto { PropertyTypeId = 1, TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.PropertyTypeId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.PropertyTypeId, context, results);
        Assert.True(isValid);
    }

    [Fact]
    public void CategoryId_WithValidValue_Validates()
    {
        var dto = new CreatePropertyDto { CategoryId = 1, TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.CategoryId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.CategoryId, context, results);
        Assert.True(isValid);
    }

    [Fact]
    public void SocietyDetailId_WithValidValue_Validates()
    {
        var dto = new CreatePropertyDto { SocietyDetailId = 1, TaxZoneId = 1, WardId = 1 };
        var context = new ValidationContext(dto) { MemberName = nameof(dto.SocietyDetailId) };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(dto.SocietyDetailId, context, results);
        Assert.True(isValid);
    }

    [Fact]
    public void NullableIds_WithNull_Validates()
    {
        var dto = new CreatePropertyDto
        {
            PropertyTypeId = null,
            CategoryId = null,
            SocietyDetailId = null,
            TaxZoneId = 1,
            WardId = 1
        };

        var context1 = new ValidationContext(dto) { MemberName = nameof(dto.PropertyTypeId) };
        var results1 = new List<ValidationResult>();
        Assert.True(Validator.TryValidateProperty(dto.PropertyTypeId, context1, results1));

        var context2 = new ValidationContext(dto) { MemberName = nameof(dto.CategoryId) };
        var results2 = new List<ValidationResult>();
        Assert.True(Validator.TryValidateProperty(dto.CategoryId, context2, results2));

        var context3 = new ValidationContext(dto) { MemberName = nameof(dto.SocietyDetailId) };
        var results3 = new List<ValidationResult>();
        Assert.True(Validator.TryValidateProperty(dto.SocietyDetailId, context3, results3));
    }
}
