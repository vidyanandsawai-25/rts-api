using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetFieldValueEntity - a dynamic (EAV-style) field value for an asset. The schema
/// collapses the former typed columns (TextValue/NumberValue/DateValue/BooleanValue) into a
/// single FieldValue string column.
/// </summary>
public class AssetFieldValueEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.Now;
        var entity = new AssetFieldValueEntity
        {
            Id = 1,
            AssetId = 10,
            FieldDefinitionId = 2,
            FieldName = "Roof Type",
            FieldValue = "RCC",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(2, entity.FieldDefinitionId);
        Assert.Equal("Roof Type", entity.FieldName);
        Assert.Equal("RCC", entity.FieldValue);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_FieldNameIsEmptyString_NotNull()
    {
        var entity = new AssetFieldValueEntity();

        Assert.Equal(string.Empty, entity.FieldName);
        Assert.Null(entity.FieldValue);
        Assert.Null(entity.FieldDefinitionId);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_AssetNavigationProperty_IsNull()
    {
        var entity = new AssetFieldValueEntity();

        Assert.Null(entity.Asset);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new AssetFieldValueEntity();

        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void DoesNotImplementIHardDeletable_DespiteHavingTheMatchingFields()
    {
        Assert.False(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetFieldValueEntity)));
    }
}
