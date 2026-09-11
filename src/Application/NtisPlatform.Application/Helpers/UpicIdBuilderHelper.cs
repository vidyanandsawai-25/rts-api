using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.Helpers;

public static class UpicIdBuilderHelper
{
    private const int UpicLength = 30;

    public static string Generate(string wardNo, string propertyNo, string? partitionNo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wardNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyNo);

        string ward = Normalize(wardNo, 4, nameof(wardNo));
        string property = Normalize(propertyNo, 10, nameof(propertyNo));
        string partition = Normalize(partitionNo ?? string.Empty, 4, nameof(partitionNo));

        string year = DateTime.UtcNow.ToString("yy");
        string suffix = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        string upic = $"{ward}{property}{partition}{year}{suffix}";

        if (upic.Length != UpicLength)
            throw new InvalidOperationException($"UPIC must be exactly {UpicLength} characters.");

        return upic;
    }

    private static string Normalize(string value, int length, string fieldName)
    {
        value = value.Trim().ToUpperInvariant();

        if (value.Length > length)
            throw new ValidationException($"{fieldName} cannot exceed {length} characters.");

        return value.PadRight(length, '0');
    }
}