using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NtisPlatform.Application.Common;

/// <summary>
/// Provides guard clauses for input validation across all application services.
/// Throws ArgumentException or ArgumentNullException with descriptive messages.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures the argument is not null
    /// </summary>
    public static T AgainstNull<T>(
        [NotNull] T? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        where T : class
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName, $"{paramName} cannot be null.");
        }

        return argument;
    }

    /// <summary>
    /// Ensures the string is not null or whitespace
    /// </summary>
    public static string AgainstNullOrWhiteSpace(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the string is not null or empty
    /// </summary>
    public static string AgainstNullOrEmpty(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the integer is greater than zero (positive)
    /// </summary>
    public static int AgainstNegativeOrZero(
        int argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument <= 0)
        {
            throw new ArgumentException($"{paramName} must be greater than zero.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the long is greater than zero (positive)
    /// </summary>
    public static long AgainstNegativeOrZero(
        long argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument <= 0)
        {
            throw new ArgumentException($"{paramName} must be greater than zero.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the integer is not negative
    /// </summary>
    public static int AgainstNegative(
        int argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument < 0)
        {
            throw new ArgumentException($"{paramName} cannot be negative.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the value is within a specified range
    /// </summary>
    public static T AgainstOutOfRange<T>(
        T argument,
        T minimum,
        T maximum,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(minimum) < 0 || argument.CompareTo(maximum) > 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                argument,
                $"{paramName} must be between {minimum} and {maximum}.");
        }

        return argument;
    }

    /// <summary>
    /// Ensures the string length is within specified range
    /// </summary>
    public static string AgainstInvalidLength(
        string argument,
        int minLength,
        int maxLength,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        AgainstNullOrEmpty(argument, paramName);

        if (argument.Length < minLength || argument.Length > maxLength)
        {
            throw new ArgumentException(
                $"{paramName} length must be between {minLength} and {maxLength} characters. Current length: {argument.Length}.",
                paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the string doesn't exceed maximum length
    /// </summary>
    public static string AgainstExceedingLength(
        string argument,
        int maxLength,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        AgainstNullOrEmpty(argument, paramName);

        if (argument.Length > maxLength)
        {
            throw new ArgumentException(
                $"{paramName} cannot exceed {maxLength} characters. Current length: {argument.Length}.",
                paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the Guid is not empty
    /// </summary>
    public static Guid AgainstEmptyGuid(
        Guid argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} cannot be an empty GUID.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the collection is not null or empty
    /// </summary>
    public static IEnumerable<T> AgainstNullOrEmpty<T>(
        [NotNull] IEnumerable<T>? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        AgainstNull(argument, paramName);

        if (!argument.Any())
        {
            throw new ArgumentException($"{paramName} cannot be empty.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the stream is not null and can be read
    /// </summary>
    public static Stream AgainstInvalidStream(
        [NotNull] Stream? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentException("File is required");
        }

        if (!argument.CanRead)
        {
            throw new ArgumentException($"{paramName} must be readable.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the email format is valid (basic check)
    /// </summary>
    public static string AgainstInvalidEmailFormat(
        string argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        AgainstNullOrWhiteSpace(argument, paramName);

        if (!argument.Contains('@') || !argument.Contains('.'))
        {
            throw new ArgumentException($"{paramName} is not a valid email format.", paramName);
        }

        return argument;
    }

    /// <summary>
    /// Ensures the file extension is valid
    /// </summary>
    public static string AgainstInvalidFileExtension(
        string fileName,
        string[] validExtensions,
        [CallerArgumentExpression(nameof(fileName))] string? paramName = null)
    {
        AgainstNullOrWhiteSpace(fileName, paramName);
        AgainstNullOrEmpty(validExtensions, nameof(validExtensions));

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !validExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"{paramName} has an invalid extension. Allowed extensions: {string.Join(", ", validExtensions)}",
                paramName);
        }

        return fileName;
    }

    /// <summary>
    /// Ensures a custom condition is met
    /// </summary>
    public static void Against(
        bool condition,
        string message,
        [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null)
    {
        if (condition)
        {
            throw new ArgumentException(message, conditionExpression);
        }
    }

    /// <summary>
    /// Validates multiple conditions and throws with all validation errors
    /// </summary>
    public static void ValidateAll(params (bool condition, string message)[] validations)
    {
        var errors = validations
            .Where(v => v.condition)
            .Select(v => v.message)
            .ToList();

        if (errors.Any())
        {
            throw new ArgumentException($"Validation failed: {string.Join("; ", errors)}");
        }
    }
}
