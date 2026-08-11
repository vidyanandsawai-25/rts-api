using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for AssetFieldValueDto / CreateAssetFieldValueDto / UpdateAssetFieldValueDto - dynamic
/// field values for assets.
/// </summary>
public class AssetFieldValueDtoTests
{
    #region AssetFieldValueDto (read)

    [Fact]
    public void AssetFieldValueDto_PropertiesGetAndSetCorrectly()
    {
        var deletionDate = DateTime.Now;
        var dto = new AssetFieldValueDto
        {
            Id = 1,
            IsActive = true,
            AssetId = 10,
            FieldDefinitionId = 2,
            FieldName = "Roof Type",
            FieldValue = "RCC",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate,
            AssetName = "Building A",
            FieldDefinitionName = "Roof Type Definition"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(10, dto.AssetId);
        Assert.Equal(2, dto.FieldDefinitionId);
        Assert.Equal("Roof Type", dto.FieldName);
        Assert.Equal("RCC", dto.FieldValue);
        Assert.True(dto.MarkedForDeletion);
        Assert.Equal(deletionDate, dto.MarkedForDeletionDate);
        Assert.Equal("Building A", dto.AssetName);
        Assert.Equal("Roof Type Definition", dto.FieldDefinitionName);
    }

    [Fact]
    public void AssetFieldValueDto_Defaults_FieldNameIsEmptyString_NotNull()
    {
        var dto = new AssetFieldValueDto();

        Assert.Equal(string.Empty, dto.FieldName);
        Assert.Null(dto.FieldValue);
        Assert.Null(dto.FieldDefinitionId);
        Assert.False(dto.MarkedForDeletion);
    }

    #endregion

    #region CreateAssetFieldValueDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 1, FieldName = "Roof Type" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingAssetId_IsInvalid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 0, FieldName = "Roof Type" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetFieldValueDto.AssetId))
            && r.ErrorMessage == "AMS_AssetFieldValue_AssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithEmptyFieldName_IsInvalid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 1, FieldName = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetFieldValueDto.FieldName))
            && r.ErrorMessage == "AMS_AssetFieldValue_FieldName_Required");
    }

    [Fact]
    public void Create_WithFieldNameExceeding100Characters_IsInvalid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 1, FieldName = new string('F', 101) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetFieldValueDto.FieldName))
            && r.ErrorMessage == "AMS_AssetFieldValue_FieldName_MaxLengthExceeded_100");
    }

    [Fact]
    public void Create_WithFieldValueExceeding500Characters_IsInvalid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 1, FieldName = "Roof Type", FieldValue = new string('V', 501) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetFieldValueDto.FieldValue))
            && r.ErrorMessage == "AMS_AssetFieldValue_FieldValue_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_WithNegativeFieldDefinitionId_IsInvalid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 1, FieldName = "Roof Type", FieldDefinitionId = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetFieldValueDto.FieldDefinitionId))
            && r.ErrorMessage == "AMS_AssetFieldValue_FieldDefinitionId_InvalidRange");
    }

    [Fact]
    public void Create_WithNullFieldDefinitionIdAndFieldValue_IsValid()
    {
        var dto = new CreateAssetFieldValueDto { AssetId = 1, FieldName = "Roof Type" };

        Assert.Empty(ValidateModel(dto));
    }

    #endregion

    #region UpdateAssetFieldValueDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetFieldValueDto { FieldName = "Roof Type" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithEmptyFieldName_IsInvalid()
    {
        var dto = new UpdateAssetFieldValueDto { FieldName = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetFieldValueDto.FieldName))
            && r.ErrorMessage == "AMS_AssetFieldValue_FieldName_Required");
    }

    [Fact]
    public void Update_WithNullId_IsValid()
    {
        // Id is an optional (int?) identifier on the update DTO - distinct from the route id.
        var dto = new UpdateAssetFieldValueDto { FieldName = "Roof Type", Id = null };

        Assert.Empty(ValidateModel(dto));
        Assert.Null(dto.Id);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
