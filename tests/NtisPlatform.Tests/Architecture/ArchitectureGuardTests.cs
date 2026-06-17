using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Infrastructure.Repositories.Property;
using Xunit;

namespace NtisPlatform.Tests.Architecture;

/// <summary>
/// Reflection-based guards that enforce Clean Architecture layer boundaries.
/// A failing test here signals a structural violation — fix the production code, not the test.
///
/// Covers items Critical #3, Critical #5, and Important #2 from the architecture review.
/// </summary>
public class ArchitectureGuardTests
{
    // ── Dependency-direction guards ──────────────────────────────────────────────────────

    /// <summary>Critical #3 — Core must be the innermost ring: no Application or Infrastructure refs.</summary>
    [Fact]
    public void Core_does_not_reference_Application_or_Infrastructure()
    {
        var coreAssembly = typeof(IPropertyBasicDetailsRepository).Assembly;
        var refs = coreAssembly.GetReferencedAssemblies()
                               .Select(a => a.Name ?? string.Empty)
                               .ToArray();

        Assert.DoesNotContain("NtisPlatform.Application", refs);
        Assert.DoesNotContain("NtisPlatform.Infrastructure", refs);
    }

    /// <summary>Critical #3 — Application must not depend on Infrastructure.</summary>
    [Fact]
    public void Application_does_not_reference_Infrastructure()
    {
        var appAssembly = typeof(PropertyBasicDetailsService).Assembly;
        var refs = appAssembly.GetReferencedAssemblies()
                              .Select(a => a.Name ?? string.Empty)
                              .ToArray();

        Assert.DoesNotContain("NtisPlatform.Infrastructure", refs);
    }

    // ── Per-tab repository structural guards ─────────────────────────────────────────────

    /// <summary>Important #2 — Every per-tab repo must inherit the shared base to prevent
    /// copy-pasted active-property queries diverging over time.</summary>
    [Theory]
    [InlineData(typeof(PropertyBasicDetailsRepository))]
    [InlineData(typeof(PropertyKycRepository))]
    [InlineData(typeof(PropertySocietyRepository))]
    [InlineData(typeof(PropertyDiscountRepository))]
    [InlineData(typeof(PropertyOldDetailsRepository))]
    public void PerTab_repositories_inherit_PropertyRepositoryBase(Type repoType)
    {
        Assert.True(
            typeof(PropertyRepositoryBase).IsAssignableFrom(repoType),
            $"{repoType.Name} must inherit PropertyRepositoryBase. " +
            "Duplicate GetActivePropertyAsync implementations cause silent divergence.");
    }

    /// <summary>Important #2 — Read-only tab repos must not take IUnitOfWork in their constructor.
    /// SaveChanges responsibility belongs to the Application service layer.</summary>
    [Theory]
    [InlineData(typeof(PropertyBasicDetailsRepository))]
    [InlineData(typeof(PropertyKycRepository))]
    [InlineData(typeof(PropertySocietyRepository))]
    [InlineData(typeof(PropertyDiscountRepository))]
    public void ReadOnly_perTab_repositories_do_not_accept_IUnitOfWork(Type repoType)
    {
        var constructors = repoType.GetConstructors();
        foreach (var ctor in constructors)
        {
            var hasUoW = ctor.GetParameters()
                .Any(p => p.ParameterType.Name.Contains("UnitOfWork",
                              StringComparison.OrdinalIgnoreCase));
            Assert.False(hasUoW,
                $"{repoType.Name} must not accept IUnitOfWork — persistence lifecycle " +
                "belongs in the Application service, not the repository.");
        }
    }

    // ── Controller thin-adapter guard ────────────────────────────────────────────────────

    /// <summary>Critical #1 — The PropertyController must carry the PropertyApiExceptionFilter
    /// attribute so actions delegate exception-to-HTTP mapping to the filter, not inline catch.</summary>
    [Fact]
    public void PropertyController_carries_PropertyApiExceptionFilter_attribute()
    {
        var controllerType = typeof(NtisPlatform.Api.Controllers.PropertyController);
        var attrs = controllerType.GetCustomAttributes(inherit: false);

        var hasFilter = attrs.Any(a =>
            a.GetType().Name.Contains("TypeFilter") ||
            a.GetType().Name.Contains("ServiceFilter") ||
            a.GetType().Name.Contains("PropertyApiExceptionFilter"));

        Assert.True(hasFilter,
            "PropertyController must declare [TypeFilter(typeof(PropertyApiExceptionFilter))] " +
            "so action methods are thin adapters without inline exception handling.");
    }
}
