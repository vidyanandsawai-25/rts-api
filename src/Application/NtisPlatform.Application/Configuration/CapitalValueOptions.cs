namespace NtisPlatform.Application.Configuration;

/// <summary>
/// Configuration options for Capital Value service.
/// Externalizes magic strings and default values.
/// </summary>
public class CapitalValueOptions
{
    public const string SectionName = "CapitalValue";

    /// <summary>
    /// Default policy code when not specified.
    /// </summary>
    public string DefaultPolicyCode { get; set; } = "NETTAX";

    /// <summary>
    /// Enable auto-calculation when CV records don't exist.
    /// </summary>
    public bool AutoCalculateIfNotExists { get; set; } = true;

    /// <summary>
    /// Maximum number of property details to process in one request.
    /// </summary>
    public int MaxPropertyDetailsPerRequest { get; set; } = 100;
}
