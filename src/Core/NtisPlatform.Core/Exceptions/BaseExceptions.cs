namespace NtisPlatform.Core.Exceptions;

/// <summary>
/// Base exception for all domain-specific exceptions in the NTIS Platform.
/// Inherit from this to create specific exception types.
/// </summary>
public abstract class NtisPlatformException : Exception
{
    /// <summary>
    /// Error code for categorizing exceptions
    /// </summary>
    public string ErrorCode { get; }

    protected NtisPlatformException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
    }

    protected NtisPlatformException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Base exception for entity not found errors
/// </summary>
public abstract class EntityNotFoundException : NtisPlatformException
{
    public string EntityType { get; }
    public object EntityId { get; }

    protected EntityNotFoundException(string entityType, object entityId, string errorCode)
        : base($"{entityType} with ID '{entityId}' was not found.", errorCode)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Base exception for validation errors
/// </summary>
public abstract class ValidationException : NtisPlatformException
{
    protected ValidationException(string message, string errorCode)
        : base(message, errorCode)
    {
    }

    protected ValidationException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
    }
}

/// <summary>
/// Base exception for business rule violations
/// </summary>
public abstract class BusinessRuleException : NtisPlatformException
{
    protected BusinessRuleException(string message, string errorCode)
        : base(message, errorCode)
    {
    }

    protected BusinessRuleException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
    }
}
