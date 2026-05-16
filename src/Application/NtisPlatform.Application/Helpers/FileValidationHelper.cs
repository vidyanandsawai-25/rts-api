using Microsoft.Extensions.Configuration;

namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Service for file validation with configurable allowed types
/// </summary>
public class FileValidationHelper
{
    private readonly HashSet<string> _allowedMimeTypes;
    private readonly HashSet<string> _allowedExtensions;

    public FileValidationHelper(IConfiguration configuration)
    {
        var mimeTypes = configuration.GetSection("FileValidation:AllowedMimeTypes").Get<string[]>() ?? new[]
        {
            "application/pdf",
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/tiff", "image/webp",
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "text/plain"
        };
        _allowedMimeTypes = new HashSet<string>(mimeTypes, StringComparer.OrdinalIgnoreCase);

        var extensions = configuration.GetSection("FileValidation:AllowedExtensions").Get<string[]>() ?? new[]
        {
            ".pdf",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp",
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt"
        };
        _allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates if the file type is allowed for upload
    /// </summary>
    public bool IsValidFileType(string contentType, string fileName)
    {
        if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return _allowedMimeTypes.Contains(contentType) && _allowedExtensions.Contains(extension);
    }

    /// <summary>
    /// Gets a user-friendly error message for invalid file types
    /// </summary>
    public string GetInvalidFileTypeMessage()
    {
        var extensionList = string.Join(", ", _allowedExtensions.OrderBy(e => e));
        return $"Invalid file type. Allowed extensions: {extensionList}";
    }

    /// <summary>
    /// Gets the list of allowed MIME types
    /// </summary>
    public IReadOnlyCollection<string> GetAllowedMimeTypes() => _allowedMimeTypes;

    /// <summary>
    /// Gets the list of allowed file extensions
    /// </summary>
    public IReadOnlyCollection<string> GetAllowedExtensions() => _allowedExtensions;
}
