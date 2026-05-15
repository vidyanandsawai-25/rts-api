using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class ReferenceValidationConfigurationTests
{
    [Fact]
    public void ForEntity_AllowsAddingChecks()
    {
        var config = new ReferenceValidationConfiguration();
        var builder = config.ForEntity<WardEntity>();

        builder.CheckReferences(
            ("First", (ctx, id) => ctx.BlockMasters.Where(b => b.WardId == id).Cast<object>()),
            ("Second", (ctx, id) => ctx.PropertyMast.Where(p => p.WardId == id).Cast<object>()));

        var built = config.Build();
        Assert.True(built.ContainsKey(typeof(WardEntity)));
        Assert.Equal(2, built[typeof(WardEntity)].Count);
        Assert.Equal("First", built[typeof(WardEntity)][0].TableName);
        Assert.Equal("Second", built[typeof(WardEntity)][1].TableName);
    }

    [Fact]
    public void ForEntity_LastCallReplacesPreviousBuilder()
    {
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<WardEntity>().CheckReferences(("A", (c, i) => Array.Empty<object>().AsQueryable()));
        config.ForEntity<WardEntity>().CheckReferences(("B", (c, i) => Array.Empty<object>().AsQueryable()));

        var built = config.Build();

        Assert.Equal("B", built[typeof(WardEntity)].Single().TableName);
    }

    [Fact]
    public void Build_ReturnsEmptyForEntity_WhenNoChecksAdded()
    {
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<WardEntity>();

        var built = config.Build();

        Assert.Empty(built[typeof(WardEntity)]);
    }

    [Fact]
    public void Build_ContainsAllRegisteredEntities()
    {
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<WardEntity>().CheckReferences(("W", (c, i) => Array.Empty<object>().AsQueryable()));
        config.ForEntity<ZoneEntity>().CheckReferences(("Z", (c, i) => Array.Empty<object>().AsQueryable()));

        var built = config.Build();

        Assert.True(built.ContainsKey(typeof(WardEntity)));
        Assert.True(built.ContainsKey(typeof(ZoneEntity)));
    }
}
