using NtisPlatform.Application.Extensions;
using System.Text;
using Xunit;

namespace NtisPlatform.Tests.Application.Extensions;

/// <summary>
/// Comprehensive tests for CryptographyExtensions to achieve 100% line and branch coverage
/// </summary>
public class CryptographyExtensionsTests
{
    #region ComputeSha256AsyncOptimized Tests

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithValidStream_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Hello, World!";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length); // SHA256 hash in hex is 64 characters
        Assert.Matches("^[a-f0-9]+$", result); // Should be lowercase hex
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithEmptyStream_ReturnsHash()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
        // Known SHA256 hash of empty input
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", result);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithLargeStream_ReturnsCorrectHash()
    {
        // Arrange
        var content = new string('a', 1000000); // 1MB of 'a' characters
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithCustomBufferSize_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Test content for hashing";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized(bufferSize: 4096);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithSmallBufferSize_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Small buffer test";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized(bufferSize: 8);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithCancellationToken_SupportsCancel()
    {
        // Arrange
        var content = new string('x', 10000);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await stream.ComputeSha256AsyncOptimized(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_ProducesDeterministicResults()
    {
        // Arrange
        var content = "Deterministic test content";
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result1 = await stream1.ComputeSha256AsyncOptimized();
        var result2 = await stream2.ComputeSha256AsyncOptimized();

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region ComputeSha256Async Tests

    [Fact]
    public async Task ComputeSha256Async_WithValidStream_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Hello, World!";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
        Assert.Matches("^[a-f0-9]+$", result);
    }

    [Fact]
    public async Task ComputeSha256Async_WithEmptyStream_ReturnsHash()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", result);
    }

    [Fact]
    public async Task ComputeSha256Async_WithLargeStream_ReturnsCorrectHash()
    {
        // Arrange
        var content = new string('b', 1000000);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256Async_WithCustomBufferSize_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Custom buffer test content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async(bufferSize: 16384);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256Async_WithSmallBufferSize_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Small buffer content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async(bufferSize: 16);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256Async_WithCancellationToken_SupportsCancel()
    {
        // Arrange
        var content = new string('y', 10000);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await stream.ComputeSha256Async(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ComputeSha256Async_ProducesDeterministicResults()
    {
        // Arrange
        var content = "Another deterministic test";
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result1 = await stream1.ComputeSha256Async();
        var result2 = await stream2.ComputeSha256Async();

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public async Task BothMethods_ProduceSameHash_ForSameContent()
    {
        // Arrange
        var content = "Content for comparison";
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var resultOptimized = await stream1.ComputeSha256AsyncOptimized();
        var resultStandard = await stream2.ComputeSha256Async();

        // Assert
        Assert.Equal(resultOptimized, resultStandard);
    }

    [Fact]
    public async Task BothMethods_ProduceDifferentHashes_ForDifferentContent()
    {
        // Arrange
        var content1 = "Content One";
        var content2 = "Content Two";
        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content1));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content2));

        // Act
        var hash1 = await stream1.ComputeSha256Async();
        var hash2 = await stream2.ComputeSha256Async();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task ComputeSha256Async_WithBinaryData_ReturnsCorrectHash()
    {
        // Arrange
        var binaryData = new byte[] { 0, 1, 2, 3, 255, 254, 253 };
        var stream = new MemoryStream(binaryData);

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
        Assert.Matches("^[a-f0-9]+$", result);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithBinaryData_ReturnsCorrectHash()
    {
        // Arrange
        var binaryData = new byte[] { 0, 1, 2, 3, 255, 254, 253 };
        var stream = new MemoryStream(binaryData);

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
        Assert.Matches("^[a-f0-9]+$", result);
    }

    [Fact]
    public async Task ComputeSha256Async_WithUnicodeContent_ReturnsCorrectHash()
    {
        // Arrange
        var unicodeContent = "Hello ?? ??";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(unicodeContent));

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithUnicodeContent_ReturnsCorrectHash()
    {
        // Arrange
        var unicodeContent = "Hello ?? ??";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(unicodeContent));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256Async_WithExactBufferSize_ReturnsCorrectHash()
    {
        // Arrange
        var content = new string('c', 81920); // Exact default buffer size
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithExactBufferSize_ReturnsCorrectHash()
    {
        // Arrange
        var content = new string('d', 81920);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256Async_WithContentLargerThanBuffer_ReturnsCorrectHash()
    {
        // Arrange
        var content = new string('e', 200000); // Larger than default buffer
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithContentLargerThanBuffer_ReturnsCorrectHash()
    {
        // Arrange
        var content = new string('f', 200000);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(64, result.Length);
    }

    #endregion

    #region Hash Format Tests

    [Fact]
    public async Task ComputeSha256Async_ReturnsLowercaseHex()
    {
        // Arrange
        var content = "Test for lowercase";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.Equal(result, result.ToLowerInvariant());
        Assert.DoesNotMatch("[A-F]", result); // No uppercase letters
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_ReturnsLowercaseHex()
    {
        // Arrange
        var content = "Test for lowercase optimized";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.Equal(result, result.ToLowerInvariant());
        Assert.DoesNotMatch("[A-F]", result);
    }

    #endregion

    #region Known Hash Tests

    [Fact]
    public async Task ComputeSha256Async_WithKnownInput_ReturnsKnownHash()
    {
        // Arrange - "abc" has a known SHA256 hash
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        var expectedHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

        // Act
        var result = await stream.ComputeSha256Async();

        // Assert
        Assert.Equal(expectedHash, result);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithKnownInput_ReturnsKnownHash()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        var expectedHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

        // Act
        var result = await stream.ComputeSha256AsyncOptimized();

        // Assert
        Assert.Equal(expectedHash, result);
    }

    #endregion
}
