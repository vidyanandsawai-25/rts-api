namespace NtisPlatform.Application.Options;

/// <summary>
/// Strongly-typed file-storage settings, bound from the "FileStorage" configuration section.
/// Injected via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> so application
/// services depend on this contract rather than on <c>IConfiguration</c> directly.
/// </summary>
public sealed class FileStorageOptions
{
    public const string Section = "FileStorage";

    /// <summary>
    /// Base directory for stored files (absolute, or relative to the app's working directory).
    /// Defaults to "Uploads".
    /// </summary>
    public string BasePath { get; set; } = "Uploads";

    /// <summary>
    /// Stream buffer size (bytes) used while hashing/copying uploads. Defaults to 80 KB.
    /// </summary>
    public int BufferSizeBytes { get; set; } = 81920;

    /// <summary>
    /// Maximum permitted upload size (bytes). Defaults to 100 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 104857600;
}
