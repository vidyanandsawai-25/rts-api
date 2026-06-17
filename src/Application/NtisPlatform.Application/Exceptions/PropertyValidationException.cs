namespace NtisPlatform.Application.Exceptions;

/// <summary>
/// Thrown by per-tab property application services when a business rule or foreign-key
/// constraint is violated (e.g. referenced master record does not exist, duplicate entry,
/// or conflicting data). Inherits from <see cref="InvalidOperationException"/> so that:
/// <list type="bullet">
///   <item>Existing <c>catch (InvalidOperationException)</c> blocks in controllers still
///         handle it and return a 400 <c>ApiResponse</c> without any response-shape change.</item>
///   <item>The global exception middleware's <c>InvalidOperationException → 400</c> mapping
///         also applies when exceptions escape the controller boundary.</item>
/// </list>
/// Once the project adopts a solution-wide custom exception + global middleware strategy this
/// class can be updated to inherit from a richer base type.
/// </summary>
public class PropertyValidationException : InvalidOperationException
{
    public PropertyValidationException(string message) : base(message) { }

    public PropertyValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
