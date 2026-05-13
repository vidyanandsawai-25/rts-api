using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
namespace NtisPlatform.Infrastructure.Services;

public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly TranslationServiceOptions _options;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(
        HttpClient httpClient,
        IOptions<TranslationServiceOptions> options,
        ILogger<TranslationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> TranslateBatchAsync(
     IEnumerable<string> texts,
     string sourceLanguage,
     string targetLanguage,
     CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>();
        var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();

        if (textList.Count == 0)
            return result;

        try
        {
            // Google Translate API supports multiple 'q' parameters for batch translation
            var requestBody = new
            {
                q = textList,  // Pass array of texts
                source = sourceLanguage,
                target = targetLanguage,
                format = "text"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("X-goog-api-key", _options.ApiKey);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Google API Batch Error: {Status} - {Content}", response.StatusCode, errorJson);

                // Return empty dictionary on failure
                return result;
            }

            var apiResult = await response.Content.ReadFromJsonAsync<GoogleTranslateResponse>(ct);
            var translations = apiResult?.Data?.Translations ?? [];

            // Map translations back to original texts (order is preserved)
            for (int i = 0; i < textList.Count && i < translations.Count; i++)
            {
                var translatedText = translations[i].TranslatedText;
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    result[textList[i]] = translatedText;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch translation failed for {Count} texts", textList.Count);
            // Return empty dictionary on exception
            return new Dictionary<string, string>();
        }

        return result;
    }

    // Response model for Google Translate API
    private class GoogleTranslateResponse
    {
        public TranslationData? Data { get; set; }
    }

    private class TranslationData
    {
        public List<Translation>? Translations { get; set; }
    }

    private class Translation
    {
        public string TranslatedText { get; set; } = string.Empty;
    }
}