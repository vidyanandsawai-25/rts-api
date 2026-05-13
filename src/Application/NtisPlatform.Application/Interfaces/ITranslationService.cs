namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// External translation service integration (Google Translate, Azure Translator, etc.)
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate multiple texts in batch
    /// </summary>
    Task<Dictionary<string, string>> TranslateBatchAsync(
        IEnumerable<string> texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken ct = default);
}