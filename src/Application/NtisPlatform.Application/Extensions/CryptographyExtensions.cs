using System.Security.Cryptography;

namespace NtisPlatform.Application.Extensions;

/// <summary>
/// Extensions for async-friendly cryptographic operations
/// </summary>
public static class CryptographyExtensions
{
    /// <summary>
    /// Computes SHA256 hash asynchronously by offloading CPU-intensive operations to background threads.
    /// Use this for very large files (>10MB) where hashing time becomes significant.
    /// For smaller files, the overhead of Task.Run may exceed the benefits.
    /// </summary>
    /// <param name="stream">Stream to hash</param>
    /// <param name="bufferSize">Buffer size for reading (default: 81920 bytes)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SHA256 hash as lowercase hex string</returns>
    public static async Task<string> ComputeSha256AsyncOptimized(
        this Stream stream, 
        int bufferSize = 81920,
        CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            // Offload CPU-intensive hashing to background thread to keep async context free
            // This is beneficial for large files where hashing takes significant time
            await Task.Run(() => sha256.TransformBlock(buffer, 0, bytesRead, null, 0), cancellationToken);
        }

        await Task.Run(() => sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0), cancellationToken);
        return Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA256 hash for a stream with proper async/await pattern.
    /// This version doesn't offload to Task.Run as the overhead typically exceeds benefits for typical buffer sizes.
    /// </summary>
    /// <param name="stream">Stream to hash</param>
    /// <param name="bufferSize">Buffer size for reading (default: 81920 bytes)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SHA256 hash as lowercase hex string</returns>
    public static async Task<string> ComputeSha256Async(
        this Stream stream,
        int bufferSize = 81920,
        CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            // TransformBlock is fast in-memory operation, typically faster than Task.Run overhead
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
    }
}
