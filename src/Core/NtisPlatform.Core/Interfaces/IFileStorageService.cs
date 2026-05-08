namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage and returns the relative path
    /// </summary>
    Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file from storage
    /// </summary>
    Task<Stream?> ReadFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if file exists
    /// </summary>
    Task<bool> FileExistsAsync(
        string relativePath,
        CancellationToken cancellationToken = default);
}
