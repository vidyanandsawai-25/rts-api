namespace NtisPlatform.Core.Exceptions;

/// <summary>
/// Exception thrown when a document binding operation is invalid
/// </summary>
public class InvalidBindingException : BusinessRuleException
{
    public string? ModuleCode { get; }
    public string? ReferenceTableName { get; }

    public InvalidBindingException(string message, string? moduleCode = null, string? referenceTableName = null)
        : base(message, "INVALID_BINDING")
    {
        ModuleCode = moduleCode;
        ReferenceTableName = referenceTableName;
    }
}

/// <summary>
/// Exception thrown when attempting to create a duplicate document binding
/// </summary>
public class DuplicateBindingException : BusinessRuleException
{
    public int DocumentId { get; }
    public string ReferenceTableName { get; }
    public object ReferenceId { get; }

    public DuplicateBindingException(int documentId, string referenceTableName, object referenceId)
        : base($"Document {documentId} is already bound to {referenceTableName} with ID {referenceId}", "DUPLICATE_BINDING")
    {
        DocumentId = documentId;
        ReferenceTableName = referenceTableName;
        ReferenceId = referenceId;
    }
}

/// <summary>
/// Exception thrown when XOR validation fails (exactly one of two values must be provided)
/// </summary>
public class XorValidationException : ValidationException
{
    public string Parameter1Name { get; }
    public string Parameter2Name { get; }

    public XorValidationException(string parameter1Name, string parameter2Name)
        : base($"Exactly one of '{parameter1Name}' or '{parameter2Name}' must be provided, not both or neither.", "XOR_VALIDATION_FAILED")
    {
        Parameter1Name = parameter1Name;
        Parameter2Name = parameter2Name;
    }
}

/// <summary>
/// Exception thrown when a path traversal attack is detected
/// </summary>
public class PathTraversalException : BusinessRuleException
{
    public string AttemptedPath { get; }

    public PathTraversalException(string attemptedPath)
        : base($"Path traversal attempt detected: {attemptedPath}", "PATH_TRAVERSAL_DETECTED")
    {
        AttemptedPath = attemptedPath;
    }
}

/// <summary>
/// Exception thrown when filename sanitization fails
/// </summary>
public class InvalidFileNameException : ValidationException
{
    public string FileName { get; }

    public InvalidFileNameException(string fileName, string reason)
        : base($"Invalid file name '{fileName}': {reason}", "INVALID_FILE_NAME")
    {
        FileName = fileName;
    }
}
