using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for file storage operations using local file system.
/// Note: Some filesystem metadata operations (File.Exists, File.Delete) are inherently synchronous
/// but are wrapped in Task.Run() to prevent blocking the thread pool and support cancellation.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;
    private readonly int _bufferSizeBytes;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _baseStoragePath = configuration.GetValue<string>("FileStorage:BasePath") ?? "Uploads";
        _bufferSizeBytes = configuration.GetValue<int>("FileStorage:BufferSizeBytes", 81920);
        _logger = logger;

        if (!Path.IsPathRooted(_baseStoragePath))
        {
            _baseStoragePath = Path.Combine(Directory.GetCurrentDirectory(), _baseStoragePath);
        }

        // Normalize to absolute path
        _baseStoragePath = Path.GetFullPath(_baseStoragePath);

        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    /// <summary>
    /// Validates that the given path is within the base storage path to prevent path traversal attacks
    /// </summary>
    private string GetSafeFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(relativePath));
        }

        // Reject rooted/absolute paths
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Absolute paths are not allowed.", nameof(relativePath));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_baseStoragePath, relativePath));
        var relativeResolvedPath = Path.GetRelativePath(_baseStoragePath, fullPath);

        // Ensure the resolved path is still within the base storage path
        if (Path.IsPathRooted(relativeResolvedPath) ||
            relativeResolvedPath.Equals("..", StringComparison.Ordinal) ||
            relativeResolvedPath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativeResolvedPath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Access to the path is denied due to path traversal attempt.");
        }

        return fullPath;
    }

    /// <summary>
    /// Sanitizes a filename by removing or replacing problematic characters, checking for reserved names,
    /// and enforcing maximum length constraints.
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must not be empty", nameof(fileName));
        }

        // Extract just the filename (no path components)
        var safeFileName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("File name must not be empty.", nameof(fileName));
        }

        // Remove leading/trailing dots and spaces
        safeFileName = safeFileName.Trim().Trim('.');

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("File name contains only invalid characters.", nameof(fileName));
        }

        // Get invalid characters for filenames
        var invalidChars = Path.GetInvalidFileNameChars();

        // Replace invalid characters with underscore
        foreach (var c in invalidChars)
        {
            safeFileName = safeFileName.Replace(c, '_');
        }

        // Replace additional problematic characters that might not be in GetInvalidFileNameChars
        var problematicChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        foreach (var c in problematicChars)
        {
            safeFileName = safeFileName.Replace(c, '_');
        }

        // Check for reserved Windows filenames
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(safeFileName);
        var reservedNames = new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        if (reservedNames.Contains(fileNameWithoutExtension.ToUpperInvariant()))
        {
            safeFileName = $"file_{safeFileName}";
        }

        // Enforce maximum filename length based on the final generated name:
        // yyyyMMdd_HHmmss_ffffff + "_" + GUID("N") + "_" + originalFileName
        const int maxFileNameLength = 255;
        const int timestampPrefixLength = 22; // "yyyyMMdd_HHmmss_ffffff"
        const int guidLength = 32; // Guid.ToString("N")
        const int separatorLength = 2; // "_" between timestamp/guid/original name
        const int maxOriginalFileNameLength = maxFileNameLength - timestampPrefixLength - guidLength - separatorLength;

        if (safeFileName.Length > maxOriginalFileNameLength)
        {
            var extension = Path.GetExtension(safeFileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
            var maxBaseNameLength = Math.Max(1, maxOriginalFileNameLength - extension.Length);
            var truncatedName = nameWithoutExt.Substring(0, Math.Min(nameWithoutExt.Length, maxBaseNameLength));
            safeFileName = truncatedName + extension;
        }

        return safeFileName;
    }

    /// <summary>
    /// Generates a unique filename using timestamp and GUID to ensure absolute uniqueness.
    /// Format: YYYYMMDD_HHmmss_ffffff_GUID_originalfilename
    /// </summary>
    /// <param name="originalFileName">The sanitized original filename</param>
    /// <returns>A guaranteed unique filename</returns>
    private string GenerateUniqueFileName(string originalFileName)
    {
        // Use local timestamp with microsecond precision
        var timestamp = DateTime.Now;
        var timestampPrefix = timestamp.ToString("yyyyMMdd_HHmmss_ffffff");

        // Use GUID without hyphens for cleaner filenames
        var guidPart = Guid.NewGuid().ToString("N");

        // Combine: timestamp + guid + original filename
        // This provides triple-layer uniqueness:
        // 1. Timestamp ensures different files at different times
        // 2. Microseconds handle simultaneous uploads
        // 3. GUID provides cryptographic uniqueness
        return $"{timestampPrefix}_{guidPart}_{originalFileName}";
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        string? fullPath = null;
        FileStream? fileStreamDest = null;

        try
        {
            var safeFileName = SanitizeFileName(fileName);
            var uniqueFileName = GenerateUniqueFileName(safeFileName);
            fullPath = Path.Combine(_baseStoragePath, uniqueFileName);

            // Use FileMode.CreateNew to ensure we never overwrite an existing file.
            // If file exists, opening the stream will throw IOException which indicates a collision.
            fileStreamDest = CreateNewFileStream(fullPath, fileName);

            await fileStream.CopyToAsync(fileStreamDest, cancellationToken);
            await fileStreamDest.DisposeAsync();
            fileStreamDest = null;

            _logger.LogDebug("File saved successfully: {FileName} as {UniqueFileName}", fileName, uniqueFileName);
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            if (fileStreamDest is not null)
            {
                await fileStreamDest.DisposeAsync();

                if (!string.IsNullOrEmpty(fullPath))
                {
                    try
                    {
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete partially saved file: {FileName} at path {FullPath}", fileName, fullPath);
                    }
                }
            }

            _logger.LogError(ex, "Error saving file: {FileName}", fileName);
            throw;
        }
    }

    private FileStream CreateNewFileStream(string fullPath, string fileName)
    {
        try
        {
            return new FileStream(
                fullPath,
                FileMode.CreateNew, // Changed from Create to CreateNew for safety
                FileAccess.Write,
                FileShare.None,
                bufferSize: _bufferSizeBytes,
                FileOptions.Asynchronous);
        }
        catch (IOException ex)
        {
            // Extremely rare: collision detected while creating a new file, log and re-throw with context.
            _logger.LogError(ex, "File collision detected for: {FileName}. This should be extremely rare.", fileName);
            throw new InvalidOperationException($"File name collision detected for '{fileName}'. Please retry the operation.", ex);
        }
    }

    /// <summary>
    /// Reads a file from storage asynchronously.
    /// Note: File.Exists() is wrapped in Task.Run() as it's a synchronous filesystem metadata operation.
    /// </summary>
    public async Task<Stream?> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetSafeFullPath(relativePath);

            // File.Exists is synchronous - wrap in Task.Run to avoid blocking and support cancellation
            var fileExists = await Task.Run(() => File.Exists(fullPath), cancellationToken);
            if (!fileExists)
            {
                _logger.LogWarning("File not found: {Path}", fullPath);
                return null;
            }

            // Return FileStream directly to avoid buffering entire file into memory
            // The caller is responsible for disposing the stream
            Stream fileStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return fileStream;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Path traversal attempt detected: {Path}", relativePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {Path}", relativePath);
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from storage asynchronously.
    /// Note: File.Exists() and File.Delete() are wrapped in Task.Run() as they are synchronous filesystem operations.
    /// </summary>
    public async Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetSafeFullPath(relativePath);

            // File system operations are synchronous - wrap in Task.Run to avoid blocking and support cancellation
            return await Task.Run(() =>
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted: {Path}", fullPath);
                    return true;
                }
                return false;
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Path traversal attempt detected: {Path}", relativePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", relativePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if a file exists in storage asynchronously.
    /// Note: File.Exists() is wrapped in Task.Run() as it's a synchronous filesystem metadata operation.
    /// </summary>
    public async Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetSafeFullPath(relativePath);
            // File.Exists is synchronous - wrap in Task.Run to avoid blocking and support cancellation
            return await Task.Run(() => File.Exists(fullPath), cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            // Path traversal attempt - file doesn't exist in valid storage
            return false;
        }
        catch (ArgumentException)
        {
            // Invalid path - file doesn't exist
            return false;
        }
    }
}
