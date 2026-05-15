using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for SocietyDetailsEntity to achieve 100% coverage
/// </summary>
public class SocietyDetailsEntityComprehensiveTests
{
    [Fact]
    public void SocietyDetailsEntity_DefaultConstructor_InitializesWithDefaults()
    {
        // Act
        var entity = new SocietyDetailsEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Null(entity.PropertyId);
        Assert.Null(entity.WingId);
        Assert.Null(entity.WingName);
        Assert.Null(entity.SocietyName);
        Assert.Null(entity.SocietyAddress);
        Assert.Null(entity.SecretaryName);
        Assert.Null(entity.ManagerName);
        Assert.Null(entity.LandOwnerName);
        Assert.Null(entity.BuilderName);
        Assert.Null(entity.SecretaryNameEnglish);
        Assert.Null(entity.SocietyNameEnglish);
        Assert.Null(entity.SocietyAddressEnglish);
        Assert.Null(entity.ManagerNameEnglish);
        Assert.Null(entity.LandOwnerNameEnglish);
        Assert.Null(entity.BuilderNameEnglish);
        Assert.Null(entity.BuilderMobileRemarkId);
        Assert.Null(entity.BuilderMobile);
        Assert.Null(entity.ManagerMobileNo);
        Assert.Null(entity.SecretaryMobileNo);
        Assert.Null(entity.SocietyEmailId);
        Assert.Null(entity.SecretaryEmailId);
        Assert.Null(entity.ManagerEmailId);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void SocietyDetailsEntity_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var now = DateTime.Now;

        // Act
        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 100,
            WingId = 5,
            WingName = "A Wing",
            SocietyName = "Green Valley Society",
            SocietyAddress = "123 Main Street, City",
            SecretaryName = "??? ?????",
            ManagerName = "???? ?????",
            LandOwnerName = "???? ?????",
            BuilderName = "???? ????????",
            SecretaryNameEnglish = "Raj Kumar",
            SocietyNameEnglish = "Green Valley Society",
            SocietyAddressEnglish = "123 Main Street, City",
            ManagerNameEnglish = "Sanjay Sharma",
            LandOwnerNameEnglish = "Vijay Patil",
            BuilderNameEnglish = "Ramesh Builders",
            BuilderMobileRemarkId = 1,
            BuilderMobile = "9876543210",
            ManagerMobileNo = "9876543211",
            SecretaryMobileNo = "9876543212",
            SocietyEmailId = "society@example.com",
            SecretaryEmailId = "secretary@example.com",
            ManagerEmailId = "manager@example.com",
            MarkedForDeletion = true,
            MarkedForDeletionDate = now,
            IsActive = true,
            CreatedBy = 10,
            CreatedDate = now,
            UpdatedBy = 20,
            UpdatedDate = now.AddHours(1)
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.PropertyId);
        Assert.Equal(5, entity.WingId);
        Assert.Equal("A Wing", entity.WingName);
        Assert.Equal("Green Valley Society", entity.SocietyName);
        Assert.Equal("123 Main Street, City", entity.SocietyAddress);
        Assert.Equal("??? ?????", entity.SecretaryName);
        Assert.Equal("???? ?????", entity.ManagerName);
        Assert.Equal("???? ?????", entity.LandOwnerName);
        Assert.Equal("???? ????????", entity.BuilderName);
        Assert.Equal("Raj Kumar", entity.SecretaryNameEnglish);
        Assert.Equal("Green Valley Society", entity.SocietyNameEnglish);
        Assert.Equal("123 Main Street, City", entity.SocietyAddressEnglish);
        Assert.Equal("Sanjay Sharma", entity.ManagerNameEnglish);
        Assert.Equal("Vijay Patil", entity.LandOwnerNameEnglish);
        Assert.Equal("Ramesh Builders", entity.BuilderNameEnglish);
        Assert.Equal(1, entity.BuilderMobileRemarkId);
        Assert.Equal("9876543210", entity.BuilderMobile);
        Assert.Equal("9876543211", entity.ManagerMobileNo);
        Assert.Equal("9876543212", entity.SecretaryMobileNo);
        Assert.Equal("society@example.com", entity.SocietyEmailId);
        Assert.Equal("secretary@example.com", entity.SecretaryEmailId);
        Assert.Equal("manager@example.com", entity.ManagerEmailId);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(10, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(20, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void SocietyDetailsEntity_WithMinimalData_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            PropertyId = 1,
            SocietyName = "Minimal Society"
        };

        // Assert
        Assert.Equal(1, entity.PropertyId);
        Assert.Equal("Minimal Society", entity.SocietyName);
        Assert.Null(entity.WingId);
        Assert.Null(entity.SecretaryName);
    }

    [Fact]
    public void SocietyDetailsEntity_WithMaxLengthWingName_WorksCorrectly()
    {
        // Arrange
        var maxLengthWingName = new string('A', 30);

        // Act
        var entity = new SocietyDetailsEntity
        {
            WingName = maxLengthWingName
        };

        // Assert
        Assert.Equal(maxLengthWingName, entity.WingName);
        Assert.Equal(30, entity.WingName.Length);
    }

    [Fact]
    public void SocietyDetailsEntity_WithMaxLengthSocietyName_WorksCorrectly()
    {
        // Arrange
        var maxLengthSocietyName = new string('B', 500);

        // Act
        var entity = new SocietyDetailsEntity
        {
            SocietyName = maxLengthSocietyName
        };

        // Assert
        Assert.Equal(maxLengthSocietyName, entity.SocietyName);
        Assert.Equal(500, entity.SocietyName.Length);
    }

    [Fact]
    public void SocietyDetailsEntity_WithMaxLengthAddress_WorksCorrectly()
    {
        // Arrange
        var maxLengthAddress = new string('C', 200);

        // Act
        var entity = new SocietyDetailsEntity
        {
            SocietyAddress = maxLengthAddress
        };

        // Assert
        Assert.Equal(maxLengthAddress, entity.SocietyAddress);
        Assert.Equal(200, entity.SocietyAddress.Length);
    }

    [Fact]
    public void SocietyDetailsEntity_WithValidMobileNumbers_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            BuilderMobile = "9876543210",
            ManagerMobileNo = "9876543211",
            SecretaryMobileNo = "9876543212"
        };

        // Assert
        Assert.Equal("9876543210", entity.BuilderMobile);
        Assert.Equal("9876543211", entity.ManagerMobileNo);
        Assert.Equal("9876543212", entity.SecretaryMobileNo);
    }

    [Fact]
    public void SocietyDetailsEntity_WithValidEmailAddresses_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            SocietyEmailId = "society@example.com",
            SecretaryEmailId = "secretary@example.com",
            ManagerEmailId = "manager@example.com"
        };

        // Assert
        Assert.Equal("society@example.com", entity.SocietyEmailId);
        Assert.Equal("secretary@example.com", entity.SecretaryEmailId);
        Assert.Equal("manager@example.com", entity.ManagerEmailId);
    }

    [Fact]
    public void SocietyDetailsEntity_WithMaxLengthEmail_WorksCorrectly()
    {
        // Arrange
        var maxEmail = new string('a', 90) + "@email.com";

        // Act
        var entity = new SocietyDetailsEntity
        {
            SocietyEmailId = maxEmail
        };

        // Assert
        Assert.Equal(maxEmail, entity.SocietyEmailId);
        Assert.True(entity.SocietyEmailId.Length <= 100);
    }

    [Fact]
    public void SocietyDetailsEntity_MarkedForDeletion_DefaultsToFalse()
    {
        // Act
        var entity = new SocietyDetailsEntity();

        // Assert
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void SocietyDetailsEntity_CanBeMarkedForDeletion_WorksCorrectly()
    {
        // Arrange
        var entity = new SocietyDetailsEntity
        {
            PropertyId = 1,
            SocietyName = "Test Society"
        };

        // Act
        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = DateTime.Now;

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void SocietyDetailsEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var entity = new SocietyDetailsEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void SocietyDetailsEntity_WithUnicodeCharacters_WorksCorrectly()
    {
        // Act - Using real Marathi/Hindi characters to validate Unicode support
        var entity = new SocietyDetailsEntity
        {
            SecretaryName = "????? ?????",
            ManagerName = "????? ?????",
            SocietyName = "???? ??? ???????"
        };

        // Assert
        Assert.Equal("????? ?????", entity.SecretaryName);
        Assert.Equal("????? ?????", entity.ManagerName);
        Assert.Equal("???? ??? ???????", entity.SocietyName);
    }

    [Fact]
    public void SocietyDetailsEntity_WithBothLanguageVersions_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            SecretaryName = "??? ?????",
            SecretaryNameEnglish = "Raj Kumar",
            SocietyName = "????? ????",
            SocietyNameEnglish = "Green Valley"
        };

        // Assert
        Assert.Equal("??? ?????", entity.SecretaryName);
        Assert.Equal("Raj Kumar", entity.SecretaryNameEnglish);
        Assert.Equal("????? ????", entity.SocietyName);
        Assert.Equal("Green Valley", entity.SocietyNameEnglish);
    }

    [Fact]
    public void SocietyDetailsEntity_EmptyStrings_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            WingName = string.Empty,
            SocietyName = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, entity.WingName);
        Assert.Equal(string.Empty, entity.SocietyName);
    }

    [Fact]
    public void SocietyDetailsEntity_WithAllContactInformation_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            BuilderMobile = "1234567890",
            ManagerMobileNo = "1234567891",
            SecretaryMobileNo = "1234567892",
            SocietyEmailId = "society@test.com",
            SecretaryEmailId = "secretary@test.com",
            ManagerEmailId = "manager@test.com"
        };

        // Assert
        Assert.NotNull(entity.BuilderMobile);
        Assert.NotNull(entity.ManagerMobileNo);
        Assert.NotNull(entity.SecretaryMobileNo);
        Assert.NotNull(entity.SocietyEmailId);
        Assert.NotNull(entity.SecretaryEmailId);
        Assert.NotNull(entity.ManagerEmailId);
    }

    [Fact]
    public void SocietyDetailsEntity_WithNullPropertyId_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            PropertyId = null,
            SocietyName = "Society Without Property"
        };

        // Assert
        Assert.Null(entity.PropertyId);
        Assert.Equal("Society Without Property", entity.SocietyName);
    }

    [Fact]
    public void SocietyDetailsEntity_WithNullWingId_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            WingId = null,
            PropertyId = 1
        };

        // Assert
        Assert.Null(entity.WingId);
        Assert.Equal(1, entity.PropertyId);
    }

    [Fact]
    public void SocietyDetailsEntity_WithSpecialCharactersInNames_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            SecretaryName = "O'Brien-Smith",
            BuilderName = "M/s. Builder & Co.",
            SocietyAddress = "123, 2nd Floor, A-Block"
        };

        // Assert
        Assert.Equal("O'Brien-Smith", entity.SecretaryName);
        Assert.Equal("M/s. Builder & Co.", entity.BuilderName);
        Assert.Equal("123, 2nd Floor, A-Block", entity.SocietyAddress);
    }

    [Fact]
    public void SocietyDetailsEntity_WithLongEmailAddresses_WorksCorrectly()
    {
        // Arrange
        var longEmail = "verylongemailaddress.with.multiple.dots@subdomain.example.com";

        // Act
        var entity = new SocietyDetailsEntity
        {
            SocietyEmailId = longEmail
        };

        // Assert
        Assert.Equal(longEmail, entity.SocietyEmailId);
    }

    [Fact]
    public void SocietyDetailsEntity_BuilderMobileRemarkId_CanBeSet()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            BuilderMobileRemarkId = 5,
            BuilderMobile = "9876543210"
        };

        // Assert
        Assert.Equal(5, entity.BuilderMobileRemarkId);
        Assert.Equal("9876543210", entity.BuilderMobile);
    }

    [Fact]
    public void SocietyDetailsEntity_WithAllNullValues_WorksCorrectly()
    {
        // Act
        var entity = new SocietyDetailsEntity
        {
            PropertyId = null,
            WingId = null,
            WingName = null,
            BuilderMobileRemarkId = null,
            MarkedForDeletionDate = null
        };

        // Assert
        Assert.Null(entity.PropertyId);
        Assert.Null(entity.WingId);
        Assert.Null(entity.WingName);
        Assert.Null(entity.BuilderMobileRemarkId);
        Assert.Null(entity.MarkedForDeletionDate);
    }
}
