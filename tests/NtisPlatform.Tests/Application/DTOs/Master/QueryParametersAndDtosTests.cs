using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;
using NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;
using NtisPlatform.Application.DTOs.Master.DepartmentMaster;
using NtisPlatform.Application.DTOs.Master.DesignationMaster;
using NtisPlatform.Application.DTOs.Master.ModuleMaster;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application.DTOs.Master;

/// <summary>
/// Comprehensive tests for QueryParameters classes to achieve 100% code coverage
/// </summary>
public class QueryParametersTests
{
    [Fact]
    public void ConfigCategoryMasterQueryParameters_AllProperties_GetSet_WorksCorrectly()
    {
        var queryParams = new ConfigCategoryMasterQueryParameters
        {
            CategoryCode = "CC001",
            CategoryName = "General",
            DisplayOrder = 1,
            IsActive = true,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "CategoryName",
            SortOrder = "asc",
            SearchTerm = "search"
        };

        Assert.Equal("CC001", queryParams.CategoryCode);
        Assert.Equal("General", queryParams.CategoryName);
        Assert.Equal(1, queryParams.DisplayOrder);
        Assert.True(queryParams.IsActive);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
        Assert.Equal("CategoryName", queryParams.SortBy);
        Assert.Equal("asc", queryParams.SortOrder);
        Assert.Equal("search", queryParams.SearchTerm);
    }

    [Fact]
    public void ConfigCategoryMasterQueryParameters_DefaultValues()
    {
        var queryParams = new ConfigCategoryMasterQueryParameters();

        Assert.Null(queryParams.CategoryCode);
        Assert.Null(queryParams.CategoryName);
        Assert.Null(queryParams.DisplayOrder);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void DepartmentLicenceDetailsQueryParameters_AllProperties_GetSet_WorksCorrectly()
    {
        var queryParams = new DepartmentLicenceDetailsQueryParameters
        {
            DepartmentId = 5,
            IsActive = true,
            LicenceDuration = "1 Year",
            PageNumber = 1,
            PageSize = 20
        };

        Assert.Equal(5, queryParams.DepartmentId);
        Assert.True(queryParams.IsActive);
        Assert.Equal("1 Year", queryParams.LicenceDuration);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(20, queryParams.PageSize);
    }

    [Fact]
    public void DepartmentLicenceDetailsQueryParameters_DefaultValues()
    {
        var queryParams = new DepartmentLicenceDetailsQueryParameters();

        Assert.Null(queryParams.DepartmentId);
        Assert.Null(queryParams.IsActive);
        Assert.Null(queryParams.LicenceDuration);
    }

    [Fact]
    public void DepartmentMasterQueryParameters_AllProperties_GetSet_WorksCorrectly()
    {
        var queryParams = new DepartmentMasterQueryParameters
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "IT Department",
            DepartmentNameLocal = "???? ?????",
            IsActive = true,
            PageNumber = 2,
            PageSize = 50
        };

        Assert.Equal("DEPT001", queryParams.DepartmentCode);
        Assert.Equal("IT Department", queryParams.DepartmentName);
        Assert.Equal("???? ?????", queryParams.DepartmentNameLocal);
        Assert.True(queryParams.IsActive);
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(50, queryParams.PageSize);
    }

    [Fact]
    public void DepartmentMasterQueryParameters_DefaultValues()
    {
        var queryParams = new DepartmentMasterQueryParameters();

        Assert.Null(queryParams.DepartmentCode);
        Assert.Null(queryParams.DepartmentName);
        Assert.Null(queryParams.DepartmentNameLocal);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void DesignationMasterQueryParameters_AllProperties_GetSet_WorksCorrectly()
    {
        var queryParams = new DesignationMasterQueryParameters
        {
            DesignationCode = "MGR",
            DesignationName = "Manager",
            DesignationLocal = "???????",
            IsActive = true,
            PageNumber = 3,
            PageSize = 15
        };

        Assert.Equal("MGR", queryParams.DesignationCode);
        Assert.Equal("Manager", queryParams.DesignationName);
        Assert.Equal("???????", queryParams.DesignationLocal);
        Assert.True(queryParams.IsActive);
        Assert.Equal(3, queryParams.PageNumber);
        Assert.Equal(15, queryParams.PageSize);
    }

    [Fact]
    public void DesignationMasterQueryParameters_DefaultValues()
    {
        var queryParams = new DesignationMasterQueryParameters();

        Assert.Null(queryParams.DesignationCode);
        Assert.Null(queryParams.DesignationName);
        Assert.Null(queryParams.DesignationLocal);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void ModuleMasterQueryParameters_AllProperties_GetSet_WorksCorrectly()
    {
        var queryParams = new ModuleMasterQueryParameters
        {
            DepartmentId = 10,
            ModuleCode = "MOD001",
            ModuleName = "Property Tax",
            ModuleNameLocal = "??????? ??",
            IsActive = true,
            PageNumber = 1,
            PageSize = 25
        };

        Assert.Equal(10, queryParams.DepartmentId);
        Assert.Equal("MOD001", queryParams.ModuleCode);
        Assert.Equal("Property Tax", queryParams.ModuleName);
        Assert.Equal("??????? ??", queryParams.ModuleNameLocal);
        Assert.True(queryParams.IsActive);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(25, queryParams.PageSize);
    }

    [Fact]
    public void ModuleMasterQueryParameters_DefaultValues()
    {
        var queryParams = new ModuleMasterQueryParameters();

        Assert.Null(queryParams.DepartmentId);
        Assert.Null(queryParams.ModuleCode);
        Assert.Null(queryParams.ModuleName);
        Assert.Null(queryParams.ModuleNameLocal);
        Assert.Null(queryParams.IsActive);
    }
}

/// <summary>
/// Comprehensive tests for CreateMultilingualDetailsDtos to achieve 100% code coverage
/// </summary>
public class MultilingualDetailsDtosTests
{
    [Fact]
    public void CreateMultilingualDetailsDtos_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateMultilingualDetailsDtos
        {
            Id = 1,
            Resource = "PropertyLabels",
            Key = "PropertyNo",
            Culture = "en-US",
            Value = "Property Number",
            IsActive = true,
            CreatedBy = 100
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("PropertyLabels", dto.Resource);
        Assert.Equal("PropertyNo", dto.Key);
        Assert.Equal("en-US", dto.Culture);
        Assert.Equal("Property Number", dto.Value);
        Assert.True(dto.IsActive);
        Assert.Equal(100, dto.CreatedBy);
    }

    [Fact]
    public void CreateMultilingualDetailsDtos_DefaultValues()
    {
        var dto = new CreateMultilingualDetailsDtos();

        Assert.Equal(0, dto.Id);
        Assert.Equal(string.Empty, dto.Resource);
        Assert.Equal(string.Empty, dto.Key);
        Assert.Equal(string.Empty, dto.Culture);
        Assert.Equal(string.Empty, dto.Value);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void UpdateMultilingualDetailsDtos_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateMultilingualDetailsDtos
        {
            Id = 1,
            Resource = "PropertyLabels",
            Key = "PropertyNo",
            Culture = "hi-IN",
            Value = "??????? ??????",
            IsActive = true,
            UpdatedBy = 200
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("PropertyLabels", dto.Resource);
        Assert.Equal("PropertyNo", dto.Key);
        Assert.Equal("hi-IN", dto.Culture);
        Assert.Equal("??????? ??????", dto.Value);
        Assert.True(dto.IsActive);
        Assert.Equal(200, dto.UpdatedBy);
    }

    [Fact]
    public void MultilingualDetailsDtos_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var dto = new MultilingualDetailsDtos
        {
            Id = 1,
            Resource = "ValidationMessages",
            Key = "Required",
            Culture = "en-US",
            Value = "This field is required",
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("ValidationMessages", dto.Resource);
        Assert.Equal("Required", dto.Key);
        Assert.Equal("en-US", dto.Culture);
        Assert.Equal("This field is required", dto.Value);
        Assert.True(dto.IsActive);
        Assert.Equal(now, dto.CreatedDate);
        Assert.Equal(now.AddHours(1), dto.UpdatedDate);
    }
}

/// <summary>
/// Comprehensive tests for DepartmentMaster DTOs to achieve 100% code coverage
/// </summary>
public class DepartmentMasterDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateDepartmentMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateDepartmentMasterDto
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "IT Department",
            DepartmentNameLocal = "???? ?????",
            DepartmentIcon = "it-icon",
            DepartmentDescription = "Information Technology Department",
            IsActive = true,
            CreatedBy = 1
        };

        Assert.Equal("DEPT001", dto.DepartmentCode);
        Assert.Equal("IT Department", dto.DepartmentName);
        Assert.Equal("???? ?????", dto.DepartmentNameLocal);
        Assert.Equal("it-icon", dto.DepartmentIcon);
        Assert.Equal("Information Technology Department", dto.DepartmentDescription);
        Assert.True(dto.IsActive);
        Assert.Equal(1, dto.CreatedBy);
    }

    [Fact]
    public void CreateDepartmentMasterDto_ValidData_PassesValidation()
    {
        var dto = new CreateDepartmentMasterDto
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "IT Department"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDepartmentMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateDepartmentMasterDto
        {
            DepartmentCode = "DEPT002",
            DepartmentName = "HR Department",
            DepartmentNameLocal = "???? ?????",
            DepartmentIcon = "hr-icon",
            DepartmentDescription = "Human Resources",
            IsActive = true,
            UpdatedBy = 2
        };

        Assert.Equal("DEPT002", dto.DepartmentCode);
        Assert.Equal("HR Department", dto.DepartmentName);
        Assert.Equal("???? ?????", dto.DepartmentNameLocal);
        Assert.Equal("hr-icon", dto.DepartmentIcon);
        Assert.Equal("Human Resources", dto.DepartmentDescription);
        Assert.True(dto.IsActive);
        Assert.Equal(2, dto.UpdatedBy);
    }

    [Fact]
    public void UpdateDepartmentMasterDto_ValidData_PassesValidation()
    {
        var dto = new UpdateDepartmentMasterDto
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "IT Department"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }
}

/// <summary>
/// Comprehensive tests for DesignationMaster DTOs to achieve 100% code coverage
/// </summary>
public class DesignationMasterDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateDesignationMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateDesignationMasterDto
        {
            DesignationCode = "MGR",
            DesignationName = "Manager",
            DesignationLocal = "???????",
            DesignationDescription = "Department Manager",
            IsActive = true,
            CreatedBy = 1
        };

        Assert.Equal("MGR", dto.DesignationCode);
        Assert.Equal("Manager", dto.DesignationName);
        Assert.Equal("???????", dto.DesignationLocal);
        Assert.Equal("Department Manager", dto.DesignationDescription);
        Assert.True(dto.IsActive);
        Assert.Equal(1, dto.CreatedBy);
    }

    [Fact]
    public void CreateDesignationMasterDto_ValidData_PassesValidation()
    {
        var dto = new CreateDesignationMasterDto
        {
            DesignationCode = "MGR",
            DesignationName = "Manager"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDesignationMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateDesignationMasterDto
        {
            DesignationCode = "DIR",
            DesignationName = "Director",
            DesignationLocal = "??????",
            DesignationDescription = "Department Director",
            IsActive = true,
            UpdatedBy = 2
        };

        Assert.Equal("DIR", dto.DesignationCode);
        Assert.Equal("Director", dto.DesignationName);
        Assert.Equal("??????", dto.DesignationLocal);
        Assert.Equal("Department Director", dto.DesignationDescription);
        Assert.True(dto.IsActive);
        Assert.Equal(2, dto.UpdatedBy);
    }
}

/// <summary>
/// Comprehensive tests for ConfigCategoryMaster DTOs to achieve 100% code coverage
/// </summary>
public class ConfigCategoryMasterDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdateConfigCategoryMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateConfigCategoryMasterDto
        {
            CategoryCode = "GENERAL",
            CategoryName = "General Configuration",
            DisplayOrder = 1,
            IsActive = true,
            UpdatedBy = 2
        };

        Assert.Equal("GENERAL", dto.CategoryCode);
        Assert.Equal("General Configuration", dto.CategoryName);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.Equal(2, dto.UpdatedBy);
    }

    [Fact]
    public void UpdateConfigCategoryMasterDto_ValidData_PassesValidation()
    {
        var dto = new UpdateConfigCategoryMasterDto
        {
            CategoryCode = "TEST",
            CategoryName = "Test Category"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateConfigCategoryMasterDto_OptionalFields_CanBeNull()
    {
        var dto = new UpdateConfigCategoryMasterDto
        {
            CategoryCode = "TEST",
            CategoryName = "Test",
            DisplayOrder = null
        };

        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void UpdateConfigCategoryMasterDto_MissingCategoryCode_FailsValidation()
    {
        var dto = new UpdateConfigCategoryMasterDto
        {
            CategoryCode = string.Empty,
            CategoryName = "Test"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "CategoryCode_Required");
    }

    [Fact]
    public void UpdateConfigCategoryMasterDto_ExceedMaxLength_FailsValidation()
    {
        var dto = new UpdateConfigCategoryMasterDto
        {
            CategoryCode = new string('A', 31),
            CategoryName = "Test"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "CategoryCode_MaxLen_30");
    }
}

