namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for loading and processing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Loads an email template and replaces placeholders with actual values
    /// </summary>
    /// <param name="templateName">Name of the template file (without extension)</param>
    /// <param name="placeholders">Dictionary of placeholder names and their replacement values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed HTML template</returns>
    Task<string> GetTemplateAsync(
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default);
}
