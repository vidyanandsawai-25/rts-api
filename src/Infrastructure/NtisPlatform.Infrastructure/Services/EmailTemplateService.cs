using System.Net;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for loading and processing email templates
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templateBasePath;

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _logger = logger;
        // Get the base directory and construct the template path
        // Templates are located in Templates/Emails/ relative to the application's base directory
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _templateBasePath = Path.Combine(baseDirectory, "Templates", "Emails");

        _logger.LogDebug("Email template base path resolved: {TemplatePath}", _templateBasePath);
    }

    public async Task<string> GetTemplateAsync(
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        if (placeholders == null)
            throw new ArgumentNullException(nameof(placeholders));

        // Ensure .html extension
        if (!templateName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            templateName += ".html";
        }

        var templatePath = Path.Combine(_templateBasePath, templateName);

        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email template not found: {TemplatePath}", templatePath);
            throw new FileNotFoundException($"Email template not found: {templateName}", templatePath);
        }

        // Read template content
        _logger.LogDebug("Loading email template: {TemplateName}", templateName);
        var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);

        // Replace placeholders with HTML-encoded values to prevent injection/markup breaking
        foreach (var placeholder in placeholders)
        {
            var key = "{{" + placeholder.Key + "}}"; // {{Key}}
            var encodedValue = WebUtility.HtmlEncode(placeholder.Value ?? string.Empty);
            templateContent = templateContent.Replace(key, encodedValue, StringComparison.OrdinalIgnoreCase);
        }

        _logger.LogDebug("Email template processed successfully: {TemplateName}", templateName);
        return templateContent;
    }
}
