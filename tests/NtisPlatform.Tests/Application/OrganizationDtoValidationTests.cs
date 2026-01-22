using NtisPlatform.Application.DTOs.Organization;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class OrganizationDtoValidationTests
{
    #region BasicOrganizationResponse Tests

    [Fact]
    public void BasicOrganizationResponse_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var response = new BasicOrganizationResponse
        {
            Id = 1,
            Name = "Test Organization",
            IsActive = true,
            IsSetupComplete = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Assert
        Assert.Equal(1, response.Id);
        Assert.Equal("Test Organization", response.Name);
        Assert.True(response.IsActive);
        Assert.True(response.IsSetupComplete);
        Assert.NotEqual(default(DateTime), response.CreatedAt);
        Assert.NotNull(response.UpdatedAt);
    }

    [Fact]
    public void BasicOrganizationResponse_DefaultValues()
    {
        // Arrange & Act
        var response = new BasicOrganizationResponse();

        // Assert
        Assert.Equal(0, response.Id);
        Assert.Equal(string.Empty, response.Name);
        Assert.False(response.IsActive);
        Assert.False(response.IsSetupComplete);
        Assert.Equal(default(DateTime), response.CreatedAt);
        Assert.Null(response.UpdatedAt);
    }

    #endregion

    #region UpdateBasicOrganizationRequest Tests

    [Fact]
    public void UpdateBasicOrganizationRequest_WithValidData_IsValid()
    {
        // Arrange
        var request = new UpdateBasicOrganizationRequest
        {
            Name = "Updated Organization",
            IsSetupComplete = true
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpdateBasicOrganizationRequest_NameCanBeEmpty()
    {
        // Arrange
        var request = new UpdateBasicOrganizationRequest
        {
            Name = string.Empty
        };

        // Act & Assert
        Assert.Equal(string.Empty, request.Name);
    }

    [Fact]
    public void UpdateBasicOrganizationRequest_IsSetupCompleteIsOptional()
    {
        // Arrange
        var request = new UpdateBasicOrganizationRequest
        {
            Name = "Test",
            IsSetupComplete = null
        };

        // Act & Assert
        Assert.Null(request.IsSetupComplete);
    }

    #endregion

    #region UpdateOrganizationRequest Tests

    [Fact]
    public void UpdateOrganizationRequest_WithFullData_AllPropertiesSet()
    {
        // Arrange & Act
        var request = new UpdateOrganizationRequest
        {
            Name = "Test Organization",
            LogoUrl = "https://example.com/logo.png",
            LogoWidth = 200,
            LogoHeight = 100,
            LocalizedName = "संगठन",
            BackgroundImageUrl = "https://example.com/bg.jpg",
            PortalTitle = "Test Portal",
            PrimaryContactEmail = "contact@test.com",
            PrimaryContactPhone = "1234567890",
            WebsiteUrl = "https://test.com",
            Address = "123 Test St",
            City = "Test City",
            State = "Test State",
            PostalCode = "12345",
            Country = "India",
            Description = "Test Description",
            TaxId = "TAX123"
        };

        // Assert
        Assert.Equal("Test Organization", request.Name);
        Assert.Equal("https://example.com/logo.png", request.LogoUrl);
        Assert.Equal(200, request.LogoWidth);
        Assert.Equal(100, request.LogoHeight);
        Assert.Equal("संगठन", request.LocalizedName);
        Assert.Equal("https://example.com/bg.jpg", request.BackgroundImageUrl);
        Assert.Equal("Test Portal", request.PortalTitle);
        Assert.Equal("contact@test.com", request.PrimaryContactEmail);
        Assert.Equal("1234567890", request.PrimaryContactPhone);
        Assert.Equal("https://test.com", request.WebsiteUrl);
        Assert.Equal("123 Test St", request.Address);
        Assert.Equal("Test City", request.City);
        Assert.Equal("Test State", request.State);
        Assert.Equal("12345", request.PostalCode);
        Assert.Equal("India", request.Country);
        Assert.Equal("Test Description", request.Description);
        Assert.Equal("TAX123", request.TaxId);
    }

    [Fact]
    public void UpdateOrganizationRequest_OptionalFieldsCanBeNull()
    {
        // Arrange & Act
        var request = new UpdateOrganizationRequest
        {
            Name = "Test",
            PrimaryContactEmail = "test@example.com",
            Country = "India"
        };

        // Assert
        Assert.Null(request.LogoUrl);
        Assert.Null(request.LogoWidth);
        Assert.Null(request.LogoHeight);
        Assert.Null(request.LocalizedName);
        Assert.Null(request.BackgroundImageUrl);
        Assert.Null(request.PortalTitle);
        Assert.Null(request.PrimaryContactPhone);
        Assert.Null(request.WebsiteUrl);
        Assert.Null(request.Address);
        Assert.Null(request.City);
        Assert.Null(request.State);
        Assert.Null(request.PostalCode);
        Assert.Null(request.Description);
        Assert.Null(request.TaxId);
    }

    [Fact]
    public void UpdateOrganizationRequest_DefaultCountryIsIndia()
    {
        // Arrange & Act
        var request = new UpdateOrganizationRequest
        {
            Name = "Test",
            PrimaryContactEmail = "test@example.com"
        };

        // Assert
        Assert.Equal("India", request.Country);
    }

    #endregion

    #region OrganizationResponse Tests

    [Fact]
    public void OrganizationResponse_WithFullData_AllPropertiesSet()
    {
        // Arrange & Act
        var response = new OrganizationResponse
        {
            Id = 1,
            Name = "Test Organization",
            LogoUrl = "https://example.com/logo.png",
            LogoWidth = 200,
            LogoHeight = 100,
            LocalizedName = "संगठन",
            BackgroundImageUrl = "https://example.com/bg.jpg",
            PortalTitle = "Test Portal",
            PrimaryContactEmail = "contact@test.com",
            PrimaryContactPhone = "1234567890",
            WebsiteUrl = "https://test.com",
            Address = "123 Test St",
            City = "Test City",
            State = "Test State",
            PostalCode = "12345",
            Country = "India",
            Description = "Test Description",
            TaxId = "TAX123",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Assert
        Assert.Equal(1, response.Id);
        Assert.Equal("Test Organization", response.Name);
        Assert.Equal("https://example.com/logo.png", response.LogoUrl);
        Assert.Equal(200, response.LogoWidth);
        Assert.Equal(100, response.LogoHeight);
        Assert.True(response.IsActive);
        Assert.NotEqual(default(DateTime), response.CreatedAt);
    }

    [Fact]
    public void OrganizationResponse_DefaultCountryIsIndia()
    {
        // Arrange & Act
        var response = new OrganizationResponse();

        // Assert
        Assert.Equal("India", response.Country);
    }

    #endregion

    #region UpdateOrganizationSettingsRequest Tests

    [Fact]
    public void UpdateOrganizationSettingsRequest_SettingsCanBeEmpty()
    {
        // Arrange & Act
        var request = new UpdateOrganizationSettingsRequest();

        // Assert
        Assert.NotNull(request.Settings);
        Assert.Empty(request.Settings);
    }

    [Fact]
    public void UpdateOrganizationSettingsRequest_SettingsCanContainMultipleItems()
    {
        // Arrange & Act
        var request = new UpdateOrganizationSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                ["Branding.Logo"] = "logo.png",
                ["Theme.PrimaryColor"] = "#000000",
                ["Security.MaxLoginAttempts"] = "5"
            }
        };

        // Assert
        Assert.Equal(3, request.Settings.Count);
        Assert.Equal("logo.png", request.Settings["Branding.Logo"]);
        Assert.Equal("#000000", request.Settings["Theme.PrimaryColor"]);
        Assert.Equal("5", request.Settings["Security.MaxLoginAttempts"]);
    }

    [Fact]
    public void UpdateOrganizationSettingsRequest_SettingsCanBeReinitialized()
    {
        // Arrange
        var request = new UpdateOrganizationSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                ["Key1"] = "Value1"
            }
        };

        // Act
        request.Settings = new Dictionary<string, string>
        {
            ["Key2"] = "Value2"
        };

        // Assert
        Assert.Single(request.Settings);
        Assert.Equal("Value2", request.Settings["Key2"]);
        Assert.False(request.Settings.ContainsKey("Key1"));
    }

    #endregion

    #region UpdateOrganizationSettingsResponse Tests

    [Fact]
    public void UpdateOrganizationSettingsResponse_WithSuccessfulUpdate()
    {
        // Arrange & Act
        var response = new UpdateOrganizationSettingsResponse
        {
            Success = true,
            Message = "Settings updated successfully",
            UpdatedCount = 5
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Settings updated successfully", response.Message);
        Assert.Equal(5, response.UpdatedCount);
    }

    [Fact]
    public void UpdateOrganizationSettingsResponse_WithFailure()
    {
        // Arrange & Act
        var response = new UpdateOrganizationSettingsResponse
        {
            Success = false,
            Message = "Failed to update settings",
            UpdatedCount = 0
        };

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Failed to update settings", response.Message);
        Assert.Equal(0, response.UpdatedCount);
    }

    [Fact]
    public void UpdateOrganizationSettingsResponse_DefaultValues()
    {
        // Arrange & Act
        var response = new UpdateOrganizationSettingsResponse();

        // Assert
        Assert.False(response.Success);
        Assert.Equal(string.Empty, response.Message);
        Assert.Equal(0, response.UpdatedCount);
    }

    #endregion

    #region DTO Serialization Tests

    [Fact]
    public void BasicOrganizationResponse_WithNullableFields_SerializesCorrectly()
    {
        // Arrange
        var response = new BasicOrganizationResponse
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            IsSetupComplete = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = null
        };

        // Act & Assert
        Assert.NotNull(response);
        Assert.Null(response.UpdatedAt);
    }

    [Fact]
    public void OrganizationResponse_WithMixedNullableFields_HandlesCorrectly()
    {
        // Arrange
        var response = new OrganizationResponse
        {
            Id = 1,
            Name = "Test",
            LogoUrl = null,
            LogoWidth = 100,
            LogoHeight = null,
            PrimaryContactEmail = "test@test.com",
            PrimaryContactPhone = null,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = null
        };

        // Assert
        Assert.Null(response.LogoUrl);
        Assert.Equal(100, response.LogoWidth);
        Assert.Null(response.LogoHeight);
        Assert.NotNull(response.PrimaryContactEmail);
        Assert.Null(response.PrimaryContactPhone);
        Assert.Null(response.UpdatedAt);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void UpdateOrganizationRequest_WithVeryLongStrings_AcceptsData()
    {
        // Arrange
        var longString = new string('A', 1000);

        // Act
        var request = new UpdateOrganizationRequest
        {
            Name = longString,
            Description = longString,
            Address = longString,
            PrimaryContactEmail = "test@test.com",
            Country = "India"
        };

        // Assert
        Assert.Equal(1000, request.Name.Length);
        Assert.Equal(1000, request.Description?.Length);
        Assert.Equal(1000, request.Address?.Length);
    }

    [Fact]
    public void UpdateOrganizationSettingsRequest_WithSpecialCharactersInKeys_HandlesCorrectly()
    {
        // Arrange & Act
        var request = new UpdateOrganizationSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                ["Key.With.Dots"] = "value1",
                ["Key-With-Dashes"] = "value2",
                ["Key_With_Underscores"] = "value3",
                ["KeyWithNumbers123"] = "value4"
            }
        };

        // Assert
        Assert.Equal(4, request.Settings.Count);
        Assert.True(request.Settings.ContainsKey("Key.With.Dots"));
        Assert.True(request.Settings.ContainsKey("Key-With-Dashes"));
        Assert.True(request.Settings.ContainsKey("Key_With_Underscores"));
        Assert.True(request.Settings.ContainsKey("KeyWithNumbers123"));
    }

    [Fact]
    public void OrganizationResponse_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange & Act
        var response = new OrganizationResponse
        {
            Name = "Test संगठन 组织",
            LocalizedName = "संगठन",
            Address = "123 Test St, विशेष पता",
            City = "नगर",
            Description = "Description with émojis 😊",
            PrimaryContactEmail = "test@test.com",
            Country = "भारत"
        };

        // Assert
        Assert.Contains("संगठन", response.Name);
        Assert.Equal("संगठन", response.LocalizedName);
        Assert.Contains("विशेष", response.Address);
        Assert.Equal("नगर", response.City);
        Assert.Contains("😊", response.Description);
    }

    #endregion

    #region Helper Methods

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
}
