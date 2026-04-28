namespace NtisPlatform.Application.Models;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors
    /// </summary>
    public List<ValidationError> Errors { get; }

    private ValidationResult()
    {
        Errors = new List<ValidationError>();
    }

    private ValidationResult(IEnumerable<ValidationError> errors)
    {
        Errors = errors.ToList();
    }

    /// <summary>
    /// Creates a successful validation result with no errors
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation</param>
    /// <param name="errorMessage">The error message describing the validation failure</param>
    public static ValidationResult Failure(string propertyName, string errorMessage)
        => new(new[] { new ValidationError(propertyName, errorMessage) });

    /// <summary>
    /// Creates a failed validation result with a single error (no property name)
    /// </summary>
    /// <param name="errorMessage">The error message describing the validation failure</param>
    public static ValidationResult Failure(string errorMessage)
        => new(new[] { new ValidationError(string.Empty, errorMessage) });

    /// <summary>
    /// Creates a failed validation result with multiple errors
    /// </summary>
    /// <param name="errors">The validation errors</param>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
        => new(errors);

    /// <summary>
    /// Creates a failed validation result from a dictionary of property names to error messages
    /// </summary>
    /// <param name="errors">Dictionary mapping property names to error messages</param>
    public static ValidationResult Failure(Dictionary<string, string> errors)
        => new(errors.Select(e => new ValidationError(e.Key, e.Value)));

    /// <summary>
    /// Converts validation errors to a dictionary format
    /// </summary>
    public Dictionary<string, string> ToDictionary()
        => Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "General" : e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g.Select(e => e.ErrorMessage)));
}

/// <summary>
/// Represents a single validation error
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation (empty for general errors)</param>
/// <param name="ErrorMessage">The error message describing the validation failure</param>
public record ValidationError(string PropertyName, string ErrorMessage);
