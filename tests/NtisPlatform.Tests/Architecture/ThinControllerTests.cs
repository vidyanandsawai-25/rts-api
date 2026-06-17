using NtisPlatform.Api.Controllers;
using NtisPlatform.Api.Filters;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Infrastructure.Repositories.Property;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace NtisPlatform.Tests.Architecture;

/// <summary>
/// Structural guards verifying that PropertyController is a thin adapter:
/// — constructor injects only Application/Core interfaces, never Infrastructure concretions
/// — carries the global exception filter so actions have no inline catch blocks
/// </summary>
public class ThinControllerTests
{
    private static readonly Type ControllerType = typeof(PropertyController);

    [Fact]
    public void PropertyController_constructor_injects_only_interfaces_no_Infrastructure_concretions()
    {
        var infra = typeof(PropertyBasicDetailsRepository).Assembly;

        var ctors = ControllerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        foreach (var ctor in ctors)
        {
            foreach (var param in ctor.GetParameters())
            {
                var paramAsm = param.ParameterType.Assembly;
                Assert.False(
                    paramAsm == infra,
                    $"PropertyController constructor takes '{param.ParameterType.FullName}' which is an " +
                    $"Infrastructure concrete type. Controllers must depend only on Application/Core interfaces.");
            }
        }
    }

    [Fact]
    public void PropertyController_carries_TypeFilter_for_PropertyApiExceptionFilter()
    {
        var attrs = ControllerType.GetCustomAttributes(inherit: false);

        var hasFilter = attrs.OfType<Microsoft.AspNetCore.Mvc.TypeFilterAttribute>()
            .Any(a => a.ImplementationType == typeof(PropertyApiExceptionFilter));

        Assert.True(hasFilter,
            "PropertyController must be annotated with [TypeFilter(typeof(PropertyApiExceptionFilter))]. " +
            "Without it, action methods will need inline catch blocks to translate exceptions — " +
            "violating the thin-adapter principle (Critical #1 & Critical #2).");
    }

    [Fact]
    public void PropertyController_CreateFromRange_has_no_user_try_catch()
    {
        // For an `async` action the C# compiler moves the body — including any user try/catch —
        // into a generated state-machine type's MoveNext(); the kickoff method's IL never carries
        // those clauses. Inspecting GetMethodBody() on the action directly is therefore a false guard.
        // We resolve the state machine and compare the exception-handling-clause count against a
        // known-thin sibling action. An async state machine always emits exactly one compiler-generated
        // clause (to funnel exceptions into the returned Task); a user try/catch adds more. Comparing
        // against a thin reference makes this robust to compiler-version clause-count drift.
        var target = ControllerType.GetMethod("CreateFromRange", BindingFlags.Public | BindingFlags.Instance);
        var thinReference = ControllerType.GetMethod("GetDashboardStats", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(target);
        Assert.NotNull(thinReference);

        var targetClauses = AsyncExceptionClauseCount(target!);
        var baselineClauses = AsyncExceptionClauseCount(thinReference!);

        Assert.True(
            targetClauses == baselineClauses,
            $"CreateFromRange appears to contain a user-authored try/catch: its async state machine has " +
            $"{targetClauses} exception-handling clause(s) vs the thin-adapter baseline of {baselineClauses}. " +
            "Controller actions must be thin adapters — exception-to-HTTP mapping belongs to PropertyApiExceptionFilter.");
    }

    /// <summary>
    /// Returns the exception-handling-clause count of an action's real body. For async methods this is
    /// the generated state machine's MoveNext(); for synchronous methods it is the method body itself.
    /// </summary>
    private static int AsyncExceptionClauseCount(MethodInfo method)
    {
        var stateMachine = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        var body = stateMachine != null
            ? stateMachine.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetMethodBody()
            : method.GetMethodBody();

        return body!.ExceptionHandlingClauses.Count;
    }

    [Fact]
    public void IPropertyMutationInvariantPolicy_is_implemented_by_PropertyMutationInvariantPolicy()
    {
        // Structural guard: the no-op seam class must implement the interface exactly once.
        var iface = typeof(NtisPlatform.Application.Interfaces.Property.IPropertyMutationInvariantPolicy);
        var impl  = typeof(PropertyMutationInvariantPolicy);

        Assert.True(iface.IsAssignableFrom(impl),
            "PropertyMutationInvariantPolicy must implement IPropertyMutationInvariantPolicy.");
    }
}
