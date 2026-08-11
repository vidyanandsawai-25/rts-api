using System;
using System.Text.RegularExpressions;

namespace NtisPlatform.Core.Models;

/// <summary>
/// Classifies a raw input string into structured property search request filters.
/// Checks patterns for Mobile numbers, UPIC IDs, exact Property numbers, and Valuation rules.
/// </summary>
public static class UnifiedQueryClassifier
{
    private static readonly Regex MobileRegex = new(@"^\d{10}$", RegexOptions.Compiled);
    private static readonly Regex UpicRegex = new(@"^[A-Z0-9\-\/]{6,20}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex NumericPropRegex = new(@"^\d{4,8}$", RegexOptions.Compiled);
    private static readonly Regex ValuationRangeRegex = new(@"^(RV|CV)\s*(>|<|=|between|more than|less than|exact)\s*(\d+)(?:\s*and\s*(\d+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static PropertySearchRequestDto? Classify(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        query = query.Trim();

        // 1. Mobile Number (exactly 10 digits)
        if (MobileRegex.IsMatch(query))
        {
            return new PropertySearchRequestDto
            {
                MobileNo = query
            };
        }

        // 2. UPIC ID (exactly 15 alphanumeric characters)
        if (UpicRegex.IsMatch(query))
        {
            return new PropertySearchRequestDto
            {
                UPICId = query
            };
        }

        // 3. Exact Property Number (4 to 8 digits)
        if (NumericPropRegex.IsMatch(query))
        {
            return new PropertySearchRequestDto
            {
                PropertyNoFrom = query,
                PropertyNoTo = query
            };
        }

        // 4. Valuation Expression (e.g. "RV > 5000", "CV between 1000 and 5000")
        var valMatch = ValuationRangeRegex.Match(query);
        if (valMatch.Success)
        {
            var method = valMatch.Groups[1].Value.ToUpper();
            var op = valMatch.Groups[2].Value.ToLower();
            if (!decimal.TryParse(valMatch.Groups[3].Value, out var val1))
            {
                return null;
            }

            var request = new PropertySearchRequestDto
            {
                ValuationMethod = method
            };

            if (op == ">" || op == "more than")
            {
                request.FilterType = "More Than";
                request.AmountValue = val1;
            }
            else if (op == "<" || op == "less than")
            {
                request.FilterType = "Less Than";
                request.AmountValue = val1;
            }
            else if (op == "=" || op == "exact")
            {
                request.FilterType = "Exact Value";
                request.AmountValue = val1;
            }
            else if (op == "between")
            {
                if (decimal.TryParse(valMatch.Groups[4].Value, out var val2))
                {
                    request.FilterType = "Between";
                    request.AmountValue = val1;
                    request.AmountTo = val2;
                }
                else
                {
                    // Invalid "between" format (missing second amount)
                    return null;
                }
            }
            return request;
        }

        // No pattern match - fall back to text query
        return null;
    }
}
