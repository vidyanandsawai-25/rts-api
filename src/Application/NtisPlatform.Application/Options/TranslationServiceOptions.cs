namespace NtisPlatform.Application.Options;

public class TranslationServiceOptions
{
    public bool IsActive { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}