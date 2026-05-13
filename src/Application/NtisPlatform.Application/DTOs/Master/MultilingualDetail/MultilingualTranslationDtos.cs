using NtisPlatform.Application.DTOs;
namespace NtisPlatform.Application.DTOs.Master.MultilingualDetail;

public class MultilingualTranslationDtos : BaseDtos
{
    public string Resource { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string en_US { get; set; } = string.Empty;
    public string hi_IN { get; set; } = string.Empty;
    public string mr_IN { get; set; } = string.Empty;
}

public class CreateMultilingualTranslationDtos : CreateBaseDtos
{
    public string Resource { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string en_US { get; set; } = string.Empty;
    public string hi_IN { get; set; } = string.Empty;
    public string mr_IN { get; set; } = string.Empty;
}

public class UpdateMultilingualTranslationDtos : UpdateBaseDtos
{
    public string Resource { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string en_US { get; set; } = string.Empty;
    public string hi_IN { get; set; } = string.Empty;
    public string mr_IN { get; set; } = string.Empty;
}
