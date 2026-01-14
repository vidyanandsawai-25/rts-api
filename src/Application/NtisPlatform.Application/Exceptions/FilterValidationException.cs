namespace NtisPlatform.Application.Exceptions;

public class FilterValidationException : Exception
{
    public Dictionary<string, string> Errors { get; }

    public FilterValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string>();
    }

    public FilterValidationException(string message, Dictionary<string, string> errors) : base(message)
    {
        Errors = errors;
    }

    public FilterValidationException(string propertyName, string errorMessage) : base($"Filter validation failed for {propertyName}")
    {
        Errors = new Dictionary<string, string> { { propertyName, errorMessage } };
    }
}
