using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Resources;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class MultilingualResourceProviderTests
{
    [Fact]
    public async Task GetAsync_ReturnsDictionary_AndCachesResult()
    {
        var mockService = new Mock<IMultilingualDetailsService>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var rows = new List<MultilingualDetailsDtos>
        {
            new MultilingualDetailsDtos { Key = "FloorID_Required", Value = "?????? ?????? ??", Culture = "hi", Resource = "ValidationMessages" },
            new MultilingualDetailsDtos { Key = "Name_Required", Value = "??? ?????? ??", Culture = "hi", Resource = "ValidationMessages" }
        };

        mockService
            .Setup(s => s.GetAllForLocalizationAsync("ValidationMessages", "hi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var provider = new MultilingualResourceProvider(mockService.Object, memoryCache);

        // First call should invoke service
        var dict = await provider.GetAsync("ValidationMessages", "hi-IN", CancellationToken.None);
        Assert.Equal(2, dict.Count);
        Assert.Equal("?????? ?????? ??", dict["FloorID_Required"]);
        Assert.Equal("??? ?????? ??", dict["Name_Required"]);

        mockService.Verify(s => s.GetAllForLocalizationAsync("ValidationMessages", "hi", It.IsAny<CancellationToken>()), Times.Once);

        // Second call with same normalized culture should use cache (no additional service call)
        var dict2 = await provider.GetAsync("ValidationMessages", "hi", CancellationToken.None);
        Assert.Equal(2, dict2.Count);
        mockService.Verify(s => s.GetAllForLocalizationAsync("ValidationMessages", "hi", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ExcludesNullOrEmptyKeysOrValues()
    {
        var mockService = new Mock<IMultilingualDetailsService>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var rows = new List<MultilingualDetailsDtos>
        {
            new MultilingualDetailsDtos { Key = "K1", Value = "V1", Culture = "en", Resource = "R" },
            new MultilingualDetailsDtos { Key = "", Value = "V2", Culture = "en", Resource = "R" },
            new MultilingualDetailsDtos { Key = null, Value = "V3", Culture = "en", Resource = "R" },
            new MultilingualDetailsDtos { Key = "K4", Value = null, Culture = "en", Resource = "R" }
        };

        mockService
            .Setup(s => s.GetAllForLocalizationAsync("R", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var provider = new MultilingualResourceProvider(mockService.Object, memoryCache);

        var dict = await provider.GetAsync("R", "en", CancellationToken.None);
        // Only K1 should be present
        Assert.Single(dict);

        Assert.True(dict.TryGetValue("K1", out var value));
        Assert.Equal("V1", value);

    }

    [Fact]
    public async Task Invalidate_RemovesCachedEntry_CausesServiceToBeCalledAgain()
    {
        var mockService = new Mock<IMultilingualDetailsService>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var rowsA = new List<MultilingualDetailsDtos>
        {
            new MultilingualDetailsDtos { Key = "K", Value = "A", Culture = "en", Resource = "R" }
        };
        var rowsB = new List<MultilingualDetailsDtos>
        {
            new MultilingualDetailsDtos { Key = "K", Value = "B", Culture = "en", Resource = "R" }
        };

        // First call returns A, second call returns B
        var seq = new Queue<List<MultilingualDetailsDtos>>(new[] { rowsA, rowsB });
        mockService
            .Setup(s => s.GetAllForLocalizationAsync("R", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => seq.Dequeue());

        var provider = new MultilingualResourceProvider(mockService.Object, memoryCache);

        var first = await provider.GetAsync("R", "en", CancellationToken.None);
        Assert.Equal("A", first["K"]);

        // Cached -> service still called only once so far
        mockService.Verify(s => s.GetAllForLocalizationAsync("R", "en", It.IsAny<CancellationToken>()), Times.Once);

        // Invalidate and call again -> should invoke service again and get new value
        provider.Invalidate("R", "en");

        var second = await provider.GetAsync("R", "en", CancellationToken.None);
        Assert.Equal("B", second["K"]);

        mockService.Verify(s => s.GetAllForLocalizationAsync("R", "en", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
