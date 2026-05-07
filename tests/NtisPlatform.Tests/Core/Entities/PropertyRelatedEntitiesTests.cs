using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for PropertyTypeMasterEntity to achieve 100% code coverage
/// </summary>
public class PropertyTypeMasterEntityTests
{
    [Fact]
    public void PropertyTypeMasterEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyTypeMasterEntity
        {
            Id = 1,
            PropertyDescription = "Residential Apartment",
            Type = "R",
            PropertyTypeGroup = "Residential",
            SearchSequence = 1,
            PropertyTypeCategoryId = 1,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("Residential Apartment", entity.PropertyDescription);
        Assert.Equal("R", entity.Type);
        Assert.Equal("Residential", entity.PropertyTypeGroup);
        Assert.Equal(1, entity.SearchSequence);
        Assert.Equal(1, entity.PropertyTypeCategoryId);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void PropertyTypeMasterEntity_InheritsFromBaseEntity()
    {
        var entity = new PropertyTypeMasterEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void PropertyTypeMasterEntity_DefaultValues_SetCorrectly()
    {
        var entity = new PropertyTypeMasterEntity();

        Assert.Equal(0, entity.Id);
        Assert.Equal(string.Empty, entity.PropertyDescription);
        Assert.Equal(string.Empty, entity.Type);
        Assert.Equal(string.Empty, entity.PropertyTypeGroup);
        Assert.Equal(0, entity.SearchSequence);
        Assert.Null(entity.PropertyTypeCategoryId);
        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyTypeMasterEntity_PropertyDescription_CanBeEmptyString()
    {
        var entity = new PropertyTypeMasterEntity
        {
            Id = 1,
            PropertyDescription = string.Empty,
            IsActive = true
        };

        Assert.Equal(string.Empty, entity.PropertyDescription);
    }

    [Fact]
    public void PropertyTypeMasterEntity_IsActive_BothValues_WorkCorrectly()
    {
        var entity1 = new PropertyTypeMasterEntity { IsActive = true };
        var entity2 = new PropertyTypeMasterEntity { IsActive = false };

        Assert.True(entity1.IsActive);
        Assert.False(entity2.IsActive);
    }

    [Fact]
    public void PropertyTypeMasterEntity_BaseEntityProperties_WorkCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyTypeMasterEntity
        {
            Id = 100,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddDays(1)
        };

        Assert.Equal(100, entity.Id);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddDays(1), entity.UpdatedDate);
    }

    [Fact]
    public void PropertyTypeMasterEntity_LongDescription_WorksCorrectly()
    {
        var longDescription = new string('A', 100);
        var entity = new PropertyTypeMasterEntity
        {
            PropertyDescription = longDescription
        };

        Assert.Equal(longDescription, entity.PropertyDescription);
        Assert.Equal(100, entity.PropertyDescription!.Length);
    }
}

/// <summary>
/// Comprehensive tests for PropertyEntity to achieve 100% code coverage
/// </summary>
public class PropertyEntityComprehensiveTests
{
    [Fact]
    public void PropertyEntity_AllPropertiesIncludingEnglish_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyEntity
        {
            Id = 549357,
            TaxZoneId = 10,
            WardId = 79,
            PropertyNo = "22",
            PartitionNo = "1",
            PropertyTypeId = 2,
            UPICId = "UPIC123",
            OpenPlot = true,
            CSN = "CSN456",
            SubZoneNo = "SZ01",
            PlotNo = "P123",
            CategoryId = 1,
            Type = "RES",
            PartType = "FULL",
            OwnerTitle = "Mr",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr",
            OwnerNameEnglish = "John English",
            OccupierTitle = "Ms",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Ms",
            OccupierNameEnglish = "Jane English",
            FlatOrShopNo = "101",
            FlatOrShopName = "Flat 101",
            FlatOrShopNoEnglish = "101Eng",
            FlatOrShopNameEnglish = "Flat English",
            Address = "123 Main St",
            Location = "Downtown",
            AddressEnglish = "123 Main Street",
            LocationEnglish = "Downtown Area",
            MobileNo = "9921759522",
            EmailId = "test@example.com",
            SocietyDetailId = 5,
            MoujaId = 3,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(549357, entity.Id);
        Assert.Equal("John English", entity.OwnerNameEnglish);
        Assert.Equal("Jane English", entity.OccupierNameEnglish);
        Assert.Equal("101Eng", entity.FlatOrShopNoEnglish);
        Assert.Equal("Flat English", entity.FlatOrShopNameEnglish);
        Assert.Equal("123 Main Street", entity.AddressEnglish);
        Assert.Equal("Downtown Area", entity.LocationEnglish);
        Assert.Equal(3, entity.MoujaId);
    }

    [Fact]
    public void PropertyEntity_IHardDeletable_Implementation()
    {
        var entity = new PropertyEntity();
        Assert.IsAssignableFrom<NtisPlatform.Core.Interfaces.IHardDeletable>(entity);
    }

    [Fact]
    public void PropertyEntity_MarkedForDeletionDate_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyEntity
        {
            MarkedForDeletion = true,
            MarkedForDeletionDate = now
        };

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyEntity_AllOptionalStringFields_CanBeNull()
    {
        var entity = new PropertyEntity
        {
            Id = 1,
            WardId = 79,
            TaxZoneId = 10,
            IsActive = true
        };

        Assert.Null(entity.OwnerTitleEnglish);
        Assert.Null(entity.OccupierTitleEnglish);
        Assert.Null(entity.FlatOrShopNoEnglish);
        Assert.Null(entity.FlatOrShopNameEnglish);
        Assert.Null(entity.AddressEnglish);
        Assert.Null(entity.LocationEnglish);
    }
}

/// <summary>
/// Comprehensive tests for PropertyDetailsEntity to achieve 100% code coverage
/// </summary>
public class PropertyDetailsEntityComprehensiveTests
{
    [Fact]
    public void PropertyDetailsEntity_AllPropertiesIncludingRenter_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            FloorId = 2,
            SubFloorId = 1,
            ConstructionYear = "2015",
            AssessmentYear = "2023",
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            CarpetAreaSqMeter = 111.48,
            CarpetAreaSqFeet = 1200.0,
            BuiltupAreaSqMeter = 139.35,
            BuiltupAreaSqFeet = 1500.0,
            NoOfRooms = 5,
            IsRenter = true,
            SubTypeOfUseId = 2,
            IsTaxable = true,
            MarkedForDeletion = false,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddHours(1)
        };

        Assert.True(entity.IsRenter);
        Assert.Equal(2, entity.SubTypeOfUseId);
        Assert.True(entity.IsTaxable);
    }

    [Fact]
    public void PropertyDetailsEntity_IsRenter_CanBeNull()
    {
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            IsActive = true
        };

        Assert.Null(entity.IsRenter);
    }

    [Fact]
    public void PropertyDetailsEntity_NavigationProperty_Renters_InitializedAsEmpty()
    {
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            IsActive = true
        };

        Assert.NotNull(entity.Renters);
        Assert.Empty(entity.Renters);
    }
}

/// <summary>
/// Comprehensive tests for PlotDetailsEntity to achieve 100% code coverage
/// </summary>
public class PlotDetailsEntityComprehensiveTests
{
    [Fact]
    public void PlotDetailsEntity_AllPropertiesExtended_GetSet_WorksCorrectly()
    {
        var entity = new PlotDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            PlotArea = 1500.25,
            PlotTaxableAreaSqFt = 1200.0,
            OpenPlotType = "R",
            OpenPlotRenterName = "John Doe",
            OpenPlotLength = 50.0,
            OpenPlotWidth = 30.0,
            PlotTaxableAreaSqMtr = 111.48,
            PlotAreaSqMtr = 139.35,
            OpenPlotSubmissionType = "Standard",
            PlotAreaMtrLength = 15.24,
            PlotAreaMtrWidth = 9.14,
            PlotAreaFtLength = 50.0,
            PlotAreaFtWidth = 30.0,
            MarkedForDeletion = false,
            IsActive = true
        };

        Assert.Equal(1200.0, entity.PlotTaxableAreaSqFt);
        Assert.Equal("R", entity.OpenPlotType);
        Assert.Equal("John Doe", entity.OpenPlotRenterName);
        Assert.Equal(50.0, entity.OpenPlotLength);
        Assert.Equal(30.0, entity.OpenPlotWidth);
        Assert.Equal(111.48, entity.PlotTaxableAreaSqMtr);
        Assert.Equal(139.35, entity.PlotAreaSqMtr);
        Assert.Equal("Standard", entity.OpenPlotSubmissionType);
    }

    [Fact]
    public void PlotDetailsEntity_OptionalPropertiesExtended_CanBeNull()
    {
        var entity = new PlotDetailsEntity
        {
            Id = 1,
            IsActive = true
        };

        Assert.Null(entity.PlotTaxableAreaSqFt);
        Assert.Null(entity.OpenPlotType);
        Assert.Null(entity.OpenPlotRenterName);
        Assert.Null(entity.OpenPlotLength);
        Assert.Null(entity.OpenPlotWidth);
        Assert.Null(entity.PlotTaxableAreaSqMtr);
        Assert.Null(entity.PlotAreaSqMtr);
        Assert.Null(entity.OpenPlotSubmissionType);
    }
}

/// <summary>
/// Comprehensive tests for UserEntity to achieve 100% code coverage
/// </summary>
public class UserEntityTests
{
    [Fact]
    public void UserEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new UserEntity
        {
            Id = 1,
            UserName = "john.doe",
            FirstName = "John",
            LastName = "Doe",
            UserCode = "USR001",
            Address = "123 Main Street",
            MobileNo = "9876543210",
            AlternateMobileNo = "8765432109",
            Email = "john.doe@example.com",
            PasswordHash = "hashedpassword123",
            MustChangePassword = true,
            Language = "en",
            Remark = "Test user",
            LockedUntilAt = now.AddHours(2),
            FailedLoginCount = 3,
            LastLoginAt = now.AddHours(-1),
            EmployeeTypeID = 2,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("john.doe", entity.UserName);
        Assert.Equal("John", entity.FirstName);
        Assert.Equal("Doe", entity.LastName);
        Assert.Equal("USR001", entity.UserCode);
        Assert.Equal("123 Main Street", entity.Address);
        Assert.Equal("9876543210", entity.MobileNo);
        Assert.Equal("8765432109", entity.AlternateMobileNo);
        Assert.Equal("john.doe@example.com", entity.Email);
        Assert.Equal("hashedpassword123", entity.PasswordHash);
        Assert.True(entity.MustChangePassword);
        Assert.Equal("en", entity.Language);
        Assert.Equal("Test user", entity.Remark);
        Assert.Equal(now.AddHours(2), entity.LockedUntilAt);
        Assert.Equal(3, entity.FailedLoginCount);
        Assert.Equal(now.AddHours(-1), entity.LastLoginAt);
        Assert.Equal(2, entity.EmployeeTypeID);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void UserEntity_InheritsFromBaseEntity()
    {
        var entity = new UserEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void UserEntity_DefaultValues_SetCorrectly()
    {
        var entity = new UserEntity();

        Assert.Equal(0, entity.Id);
        Assert.Equal(string.Empty, entity.UserName);
        Assert.Null(entity.FirstName);
        Assert.Null(entity.LastName);
        Assert.False(entity.MustChangePassword);
        Assert.Null(entity.FailedLoginCount);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void UserEntity_OptionalFields_CanBeNull()
    {
        var entity = new UserEntity
        {
            Id = 1,
            IsActive = true
        };

        Assert.Null(entity.UserCode);
        Assert.Null(entity.Address);
        Assert.Null(entity.AlternateMobileNo);
        Assert.Null(entity.Email);
        Assert.Null(entity.PasswordHash);
        Assert.Null(entity.Language);
        Assert.Null(entity.Remark);
        Assert.Null(entity.LockedUntilAt);
        Assert.Null(entity.LastLoginAt);
    }

    [Fact]
    public void UserEntity_FailedLoginCount_GetSet_WorksCorrectly()
    {
        var entity = new UserEntity
        {
            FailedLoginCount = 5
        };

        Assert.Equal(5, entity.FailedLoginCount);
    }

    [Fact]
    public void UserEntity_MustChangePassword_GetSet_WorksCorrectly()
    {
        var entity1 = new UserEntity { MustChangePassword = true };
        var entity2 = new UserEntity { MustChangePassword = false };

        Assert.True(entity1.MustChangePassword);
        Assert.False(entity2.MustChangePassword);
    }

    [Fact]
    public void UserEntity_LockoutFeature_WorksCorrectly()
    {
        var lockoutTime = DateTime.Now.AddHours(1);
        var entity = new UserEntity
        {
            LockedUntilAt = lockoutTime,
            FailedLoginCount = 5
        };

        Assert.Equal(lockoutTime, entity.LockedUntilAt);
        Assert.Equal(5, entity.FailedLoginCount);
    }
}

/// <summary>
/// Comprehensive tests for ScreenMasterEntity to achieve 100% code coverage
/// </summary>
public class ScreenMasterEntityTests
{
    [Fact]
    public void ScreenMasterEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new ScreenMasterEntity
        {
            Id = 1,
            ScreenGroupId = 5,
            ModuleId = 10,
            ScreenCode = "SCR001",
            ScreenName = "Dashboard",
            ScreenNameLocal = "????????",
            ScreenIcon = "dashboard-icon",
            RoutePath = "/dashboard",
            IsMenu = true,
            IsAuthenticationRequired = true,
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.ScreenGroupId);
        Assert.Equal(10, entity.ModuleId);
        Assert.Equal("SCR001", entity.ScreenCode);
        Assert.Equal("Dashboard", entity.ScreenName);
        Assert.Equal("????????", entity.ScreenNameLocal);
        Assert.Equal("dashboard-icon", entity.ScreenIcon);
        Assert.Equal("/dashboard", entity.RoutePath);
        Assert.True(entity.IsMenu);
        Assert.True(entity.IsAuthenticationRequired);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void ScreenMasterEntity_InheritsFromBaseEntity()
    {
        var entity = new ScreenMasterEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void ScreenMasterEntity_NavigationProperties_GetSet_WorksCorrectly()
    {
        var screenGroup = new ScreenGroupMasterEntity { Id = 5 };
        var module = new ModuleMasterEntity { Id = 10 };

        var entity = new ScreenMasterEntity
        {
            Id = 1,
            ScreenGroupId = 5,
            ModuleId = 10,
            ScreenGroup = screenGroup,
            Module = module
        };

        Assert.NotNull(entity.ScreenGroup);
        Assert.Equal(5, entity.ScreenGroup.Id);
        Assert.NotNull(entity.Module);
        Assert.Equal(10, entity.Module.Id);
    }

    [Fact]
    public void ScreenMasterEntity_NavigationProperties_CanBeNull()
    {
        var entity = new ScreenMasterEntity
        {
            Id = 1,
            ScreenGroupId = 5,
            ModuleId = 10
        };

        Assert.Null(entity.ScreenGroup);
        Assert.Null(entity.Module);
    }

    [Fact]
    public void ScreenMasterEntity_DefaultValues_SetCorrectly()
    {
        var entity = new ScreenMasterEntity();

        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.ScreenGroupId);
        Assert.Null(entity.ModuleId);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void ScreenMasterEntity_OptionalFields_CanBeNull()
    {
        var entity = new ScreenMasterEntity
        {
            Id = 1,
            IsActive = true
        };

        Assert.Null(entity.ScreenCode);
        Assert.Null(entity.ScreenName);
        Assert.Null(entity.ScreenNameLocal);
        Assert.Null(entity.ScreenIcon);
        Assert.Null(entity.RoutePath);
        Assert.Null(entity.IsMenu);
        Assert.Null(entity.IsAuthenticationRequired);
        Assert.Null(entity.DisplayOrder);
    }

    [Fact]
    public void ScreenMasterEntity_BooleanFlags_GetSet_WorksCorrectly()
    {
        var entity = new ScreenMasterEntity
        {
            IsMenu = true,
            IsAuthenticationRequired = false
        };

        Assert.True(entity.IsMenu);
        Assert.False(entity.IsAuthenticationRequired);
    }
}

