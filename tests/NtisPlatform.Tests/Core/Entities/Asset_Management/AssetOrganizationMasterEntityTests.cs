using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetOrganizationMasterEntity. Note: this file lives under
/// Core/Entities/Asset_Management/ but is declared in the NtisPlatform.Core.Entities.Master
/// namespace (matching the sibling InventoryItemCategory/Model/Name master entities in this
/// same batch, which are also physically under Asset_Management but namespaced Master).
/// </summary>
public class AssetOrganizationMasterEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.UtcNow;
        var entity = new AssetOrganizationMasterEntity
        {
            Id = 1,
            AuthorityId = 5,
            OrganizationCode = "ORG-1",
            OrganizationName = "Municipal Corporation",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.AuthorityId);
        Assert.Equal("ORG-1", entity.OrganizationCode);
        Assert.Equal("Municipal Corporation", entity.OrganizationName);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_OrganizationCodeAndName_AreEmptyString_NotNull()
    {
        var entity = new AssetOrganizationMasterEntity();

        Assert.Equal(string.Empty, entity.OrganizationCode);
        Assert.Equal(string.Empty, entity.OrganizationName);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetOrganizationMasterEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new AssetOrganizationMasterEntity();
        var now = DateTime.UtcNow;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new AssetOrganizationMasterEntity();

        Assert.True(entity.IsActive);
    }
}
