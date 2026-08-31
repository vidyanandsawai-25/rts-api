namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// One row per software field. <see cref="LabelName"/> is the system default label; the three
/// name columns are officer-editable display aliases shown in place of <see cref="LabelName"/>
/// on live screens.
/// </summary>
public class AliasMasterEntity : BaseEntity
{
    /// <summary>PascalCase field identifier, e.g. "PropertyNo".</summary>
    public string KeyName { get; set; } = string.Empty;

    public string LabelName { get; set; } = string.Empty;

    public string? EnglishName { get; set; }

    public string? RegionalName { get; set; }

    public string? HindiName { get; set; }
}
