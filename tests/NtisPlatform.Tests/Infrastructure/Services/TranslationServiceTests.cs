using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NtisPlatform.Application.Options;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class TranslationServiceTests
{
    private static TranslationService Build(HttpResponseMessage? response, out Mock<HttpMessageHandler> handler, Exception? exception = null)
    {
        handler = new Mock<HttpMessageHandler>();
        var setup = handler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        if (exception != null)
            setup.ThrowsAsync(exception);
        else if (response != null)
            setup.ReturnsAsync(response);

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test/") };
        var options = Options.Create(new TranslationServiceOptions
        {
            IsActive = true,
            ApiKey = "key",
            ApiUrl = "https://test/translate"
        });
        var logger = new Mock<ILogger<TranslationService>>();
        return new TranslationService(client, options, logger.Object);
    }

    [Fact]
    public async Task TranslateBatchAsync_ReturnsEmpty_WhenInputIsEmpty()
    {
        var service = Build(null, out _);

        var result = await service.TranslateBatchAsync(Array.Empty<string>(), "en", "hi");

        Assert.Empty(result);
    }

    [Fact]
    public async Task TranslateBatchAsync_ReturnsEmpty_WhenAllInputsBlank()
    {
        var service = Build(null, out _);

        var result = await service.TranslateBatchAsync(new[] { "", "   ", null! }, "en", "hi");

        Assert.Empty(result);
    }

    [Fact]
    public async Task TranslateBatchAsync_ReturnsTranslations_OnSuccess()
    {
        const string json = "{\"data\":{\"translations\":[{\"translatedText\":\"नमस्ते\"},{\"translatedText\":\"विदा\"}]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = Build(response, out _);

        var result = await service.TranslateBatchAsync(new[] { "Hello", "Goodbye" }, "en", "hi");

        Assert.Equal(2, result.Count);
        Assert.Equal("नमस्ते", result["Hello"]);
        Assert.Equal("विदा", result["Goodbye"]);
    }

    [Fact]
    public async Task TranslateBatchAsync_ReturnsEmpty_OnNonSuccessStatus()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"bad\"}", Encoding.UTF8, "application/json")
        };
        var service = Build(response, out _);

        var result = await service.TranslateBatchAsync(new[] { "Hello" }, "en", "hi");

        Assert.Empty(result);
    }

    [Fact]
    public async Task TranslateBatchAsync_ReturnsEmpty_OnException()
    {
        var service = Build(null, out _, exception: new HttpRequestException("network down"));

        var result = await service.TranslateBatchAsync(new[] { "Hello" }, "en", "hi");

        Assert.Empty(result);
    }

    [Fact]
    public async Task TranslateBatchAsync_DeduplicatesInputs()
    {
        const string json = "{\"data\":{\"translations\":[{\"translatedText\":\"नमस्ते\"}]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = Build(response, out _);

        var result = await service.TranslateBatchAsync(new[] { "Hello", "Hello", "Hello" }, "en", "hi");

        Assert.Single(result);
        Assert.Equal("नमस्ते", result["Hello"]);
    }

    [Fact]
    public async Task TranslateBatchAsync_SkipsEmptyTranslatedText()
    {
        const string json = "{\"data\":{\"translations\":[{\"translatedText\":\"\"},{\"translatedText\":\"विदा\"}]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = Build(response, out _);

        var result = await service.TranslateBatchAsync(new[] { "Hello", "Goodbye" }, "en", "hi");

        Assert.Single(result);
        Assert.Equal("विदा", result["Goodbye"]);
    }

    [Fact]
    public async Task TranslateBatchAsync_HandlesShortTranslationsList()
    {
        // Server returns fewer translations than input - service maps in order
        const string json = "{\"data\":{\"translations\":[{\"translatedText\":\"नमस्ते\"}]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = Build(response, out _);

        var result = await service.TranslateBatchAsync(new[] { "Hello", "Goodbye" }, "en", "hi");

        Assert.Single(result);
        Assert.Equal("नमस्ते", result["Hello"]);
    }

    [Fact]
    public async Task TranslateBatchAsync_HandlesNullPayload()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var service = Build(response, out _);

        var result = await service.TranslateBatchAsync(new[] { "Hello" }, "en", "hi");

        Assert.Empty(result);
    }
}
