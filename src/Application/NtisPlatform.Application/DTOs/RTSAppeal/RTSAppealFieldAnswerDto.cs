namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Dynamic form field question and citizen answer representation for pre-filing and process drawer.
/// </summary>
public class RTSAppealFieldAnswerDto
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
    public string? DisplayValue { get; set; }
}
