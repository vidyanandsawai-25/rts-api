namespace NtisPlatform.Api.Constants;

/// <summary>
/// Constants for file upload configuration.
/// File size limits may be configured at runtime via appsettings.json (FileStorage:MaxFileSizeBytes)
/// and applied through FormOptions in ServiceCollectionExtensions.
/// This default value may also be used by request-limit attributes where a compile-time constant is required.
/// </summary>
public static class FileUploadConstants
{
    /// <summary>
    /// Default maximum file size in bytes (100MB).
    /// This value may be used as a default and by request-size limit attributes; the effective runtime limit may also be configured in appsettings.json under FileStorage:MaxFileSizeBytes.
    /// </summary>
    public const long MaxFileSizeBytes = 104857600; // 100MB
}
