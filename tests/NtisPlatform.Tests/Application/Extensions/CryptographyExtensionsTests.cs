using System.Text;
using NtisPlatform.Application.Extensions;
using Xunit;

namespace NtisPlatform.Tests.Application.Extensions;

public class CryptographyExtensionsTests
{
    // sha256("hello") = 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824
    private const string HelloSha256 = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    [Fact]
    public async Task ComputeSha256Async_MatchesKnownValue()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("hello"));

        var hash = await stream.ComputeSha256Async();

        Assert.Equal(HelloSha256, hash);
    }

    [Fact]
    public async Task ComputeSha256Async_EmptyStream_ReturnsEmptyHash()
    {
        // sha256 of empty input
        const string empty = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        using var stream = new MemoryStream();

        var hash = await stream.ComputeSha256Async();

        Assert.Equal(empty, hash);
    }

    [Fact]
    public async Task ComputeSha256Async_WithSmallBuffer_StillProducesCorrectHash()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("hello"));

        var hash = await stream.ComputeSha256Async(bufferSize: 1);

        Assert.Equal(HelloSha256, hash);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_MatchesKnownValue()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("hello"));

        var hash = await stream.ComputeSha256AsyncOptimized();

        Assert.Equal(HelloSha256, hash);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_WithSmallBuffer_StillProducesCorrectHash()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("hello"));

        var hash = await stream.ComputeSha256AsyncOptimized(bufferSize: 1);

        Assert.Equal(HelloSha256, hash);
    }

    [Fact]
    public async Task ComputeSha256AsyncOptimized_EmptyStream_ReturnsEmptyHash()
    {
        const string empty = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        using var stream = new MemoryStream();

        var hash = await stream.ComputeSha256AsyncOptimized();

        Assert.Equal(empty, hash);
    }
}
