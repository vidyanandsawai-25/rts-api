using NtisPlatform.Application.Services.CommonDetails;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Unit tests for the central table → entity mapping registry that replaced the scattered
/// hardcoded table-name literals.
/// </summary>
public class BulkUpdateTargetRegistryTests
{
    [Theory]
    [InlineData("PTIS.PropertyMast", typeof(PropertyEntity), "Id")]
    [InlineData("PTIS.SocietyDetailsMast", typeof(SocietyDetailsEntity), "PropertyId")]
    [InlineData("PTIS.PropertyMastDetails", typeof(PropertyAssessmentEntity), "PropertyId")]
    [InlineData("PTIS.PropertyDetails", typeof(PropertyDetailsEntity), "PropertyId")]
    public void TryResolve_KnownTable_ReturnsExpectedTarget(string table, Type expectedType, string expectedKey)
    {
        var found = BulkUpdateTargetRegistry.TryResolve(table, out var target);

        Assert.True(found);
        Assert.Equal(expectedType, target.EntityType);
        Assert.Equal(expectedKey, target.KeyProperty);
    }

    [Fact]
    public void TryResolve_IsCaseInsensitive()
    {
        Assert.True(BulkUpdateTargetRegistry.TryResolve("ptis.propertymast", out var target));
        Assert.Equal(typeof(PropertyEntity), target.EntityType);
    }

    [Fact]
    public void TryResolve_UnknownTable_ReturnsFalse()
    {
        Assert.False(BulkUpdateTargetRegistry.TryResolve("PTIS.SomeOtherTable", out _));
    }

    [Fact]
    public void IsPropertyKeyedById_TrueOnlyForPropertyMast()
    {
        BulkUpdateTargetRegistry.TryResolve("PTIS.PropertyMast", out var propertyMast);
        BulkUpdateTargetRegistry.TryResolve("PTIS.PropertyDetails", out var details);

        Assert.True(BulkUpdateTargetRegistry.IsPropertyKeyedById(propertyMast));
        Assert.False(BulkUpdateTargetRegistry.IsPropertyKeyedById(details));
    }
}
