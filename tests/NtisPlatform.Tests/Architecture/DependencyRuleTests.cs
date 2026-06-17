using NetArchTest.Rules;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Infrastructure.Repositories.Property;
using Xunit;

namespace NtisPlatform.Tests.Architecture;

/// <summary>
/// NetArchTest-based dependency-direction guards.
/// Any failure here is a production-code structural violation — fix the source, not the test.
/// </summary>
public class DependencyRuleTests
{
    private static readonly Types CoreTypes = Types.InAssembly(typeof(IPropertyBasicDetailsRepository).Assembly);
    private static readonly Types AppTypes  = Types.InAssembly(typeof(PropertyBasicDetailsService).Assembly);
    private static readonly Types InfraTypes = Types.InAssembly(typeof(PropertyBasicDetailsRepository).Assembly);
    private static readonly Types ApiTypes  = Types.InAssembly(typeof(NtisPlatform.Api.Controllers.PropertyController).Assembly);

    [Fact]
    public void Core_must_not_depend_on_Application()
    {
        var result = CoreTypes
            .That().ResideInNamespace("NtisPlatform.Core")
            .ShouldNot().HaveDependencyOn("NtisPlatform.Application")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Core layer must not reference Application. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Core_must_not_depend_on_Infrastructure()
    {
        var result = CoreTypes
            .That().ResideInNamespace("NtisPlatform.Core")
            .ShouldNot().HaveDependencyOn("NtisPlatform.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Core layer must not reference Infrastructure. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_must_not_depend_on_Infrastructure()
    {
        var result = AppTypes
            .That().ResideInNamespace("NtisPlatform.Application")
            .ShouldNot().HaveDependencyOn("NtisPlatform.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application layer must not reference Infrastructure. Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_must_not_depend_on_Api_Presentation()
    {
        var result = AppTypes
            .That().ResideInNamespace("NtisPlatform.Application")
            .ShouldNot().HaveDependencyOn("NtisPlatform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application layer must not reference Presentation (NtisPlatform.Api). Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_must_not_depend_on_Api_Presentation()
    {
        var result = InfraTypes
            .That().ResideInNamespace("NtisPlatform.Infrastructure")
            .ShouldNot().HaveDependencyOn("NtisPlatform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure layer must not reference Presentation (NtisPlatform.Api). Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Controllers_must_not_directly_depend_on_Infrastructure()
    {
        var result = ApiTypes
            .That().ResideInNamespace("NtisPlatform.Api.Controllers")
            .ShouldNot().HaveDependencyOn("NtisPlatform.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Controllers must depend only on Application/Core interfaces — no direct Infrastructure references. " +
            $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
