using System.Security.Cryptography;
using System.Text;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Services.CapitalValue.Utils;

/// <summary>
/// Generates a SHA256 hash from PropertyDetails input fields used in CV calculation.
/// The hash is used to detect changes that require CV recalculation.
/// </summary>
public static class CVInputHashGenerator
{
    /// <summary>
    /// Generates a SHA256 hash from property details and property-level data.
    /// </summary>
    /// <param name="propertyDetails">The property details entity</param>
    /// <param name="hasLift">Whether the property has a lift (from FlagMaster)</param>
    /// <param name="moujaId">Mouja ID from property (affects rate master lookup)</param>
    /// <param name="csn">CSN from property (affects rate master lookup)</param>
    /// <returns>SHA256 hash string (64 characters hex)</returns>
    /// <remarks>
    /// Input string format:
    /// FloorId|SubFloorId|ConstructionYear|AssessmentYear|ConstructionTypeId|TypeOfUseId|SubTypeOfUseId|CarpetAreaSqMeter|BuiltupAreaSqMeter|HasLift|MoujaId|CSN
    /// 
    /// Example:
    /// 67|2|2021|2026|4002|8|5192|163.00|195.60|1|1001|ABC123
    /// 
    /// All nullable fields are represented as "0" or empty string if null.
    /// Decimal values are formatted with 2 decimal places for consistency.
    /// </remarks>
    public static string GenerateHash( PropertyDetailsEntity propertyDetails,  bool hasLift,  int moujaId,  string csn)
    {
        // Build input string from all critical CV calculation fields
        var inputString = BuildInputString(propertyDetails, hasLift, moujaId, csn);

        // Generate SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputString));

        // Convert to hex string
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Builds the input string from property details fields.
    /// Order and format must remain consistent for hash stability.
    /// </summary>
    private static string BuildInputString( PropertyDetailsEntity pd,  bool hasLift,  int moujaId,  string csn)
    {
        var parts = new[]
        {
            pd.FloorId.ToString(),
            pd.SubFloorId?.ToString() ?? "0",
            pd.ConstructionYear ?? "",
            pd.AssessmentYear ?? "",
            pd.ConstructionTypeId.ToString(),
            pd.TypeOfUseId.ToString(),
            pd.SubTypeOfUseId?.ToString() ?? "0",
            FormatDecimal(pd.CarpetAreaSqMeter),
            FormatDecimal(pd.BuiltupAreaSqMeter),
            hasLift ? "1" : "0",
            moujaId.ToString(),
            csn ?? ""
        };

        return string.Join("|", parts);
    }

    /// <summary>
    /// Formats a nullable double value consistently for hash generation.
    /// Always uses 2 decimal places, null becomes "0.00".
    /// </summary>
    private static string FormatDecimal(double? value)
    {
        return (value ?? 0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}
