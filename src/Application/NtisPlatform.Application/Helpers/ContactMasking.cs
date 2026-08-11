namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Partially masks contact details for display, so a user can confirm "yes, that's my email/phone"
/// without exposing the full value to whoever is looking at the screen.
/// </summary>
public static class ContactMasking
{
    /// <summary>
    /// Masks an email address, e.g. "jo***@example.com".
    /// </summary>
    public static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "***";
        }

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];
        var visibleLength = Math.Min(2, localPart.Length);
        return $"{localPart[..visibleLength]}***{domainPart}";
    }

    /// <summary>
    /// Masks a phone number, showing only the last 2 digits, e.g. "*******91".
    /// </summary>
    public static string MaskMobile(string mobileNo)
    {
        var digitsOnly = mobileNo.Length;
        if (digitsOnly <= 2)
        {
            return new string('*', digitsOnly);
        }

        var visibleLength = 2;
        var maskedLength = mobileNo.Length - visibleLength;
        return new string('*', maskedLength) + mobileNo[^visibleLength..];
    }
}
