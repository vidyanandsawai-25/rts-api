using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.Exceptions;

/// <summary>
/// Exception thrown when validation fails during a CRUD operation
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors as a dictionary of property names to error messages
    /// </summary>
    public Dictionary<string, string> Errors { get; }

    /// <summary>
    /// Gets the type of operation that failed validation
    /// </summary>
    public OperationType OperationType { get; }

    /// <summary>
    /// Creates a new ValidationException with a message and operation type
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="operationType">The type of operation that failed</param>
    public ValidationException(string message, OperationType operationType) : base(message)
    {
        Errors = new Dictionary<string, string>();
        OperationType = operationType;
    }

    /// <summary>
    /// Creates a new ValidationException with errors dictionary and operation type
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errors">Dictionary of property names to error messages</param>
    /// <param name="operationType">The type of operation that failed</param>
    public ValidationException(string message, Dictionary<string, string> errors, OperationType operationType) : base(message)
    {
        Errors = errors;
        OperationType = operationType;
    }

    /// <summary>
    /// Creates a new ValidationException with a single property error
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation</param>
    /// <param name="errorMessage">The validation error message</param>
    /// <param name="operationType">The type of operation that failed</param>
    public ValidationException(string propertyName, string errorMessage, OperationType operationType)
        : base($"Validation failed for {propertyName}: {errorMessage}")
    {
        Errors = new Dictionary<string, string> { { propertyName, errorMessage } };
        OperationType = operationType;
    }
}
