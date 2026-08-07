namespace NtisPlatform.Application.DTOs.Master;

/// <summary>
/// One selectable calculation mode, sourced from PTIS.TaxCalculationModeMaster rather than a
/// hardcoded list. The <c>Uses*Config</c> flags travel with it so callers (including the UI) can
/// decide which configuration surfaces apply WITHOUT branching on <see cref="ModeCode"/>.
/// </summary>
public class TaxCalculationModeDto
{
    public int Id { get; set; }
    public string ModeCode { get; set; } = null!;
    public string ModeName { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public bool UsesValueConfig { get; set; }
    public bool UsesConditionConfig { get; set; }
    public bool UsesMasterConfig { get; set; }
    public bool UsesHybridConfig { get; set; }
}
