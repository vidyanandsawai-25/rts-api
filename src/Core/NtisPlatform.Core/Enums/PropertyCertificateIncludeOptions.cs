namespace NtisPlatform.Core.Enums;

/// <summary>
/// Defines the loading strategy for PropertyCertificate entity navigation properties
/// </summary>
[Flags]
public enum PropertyCertificateIncludeOptions
{
    /// <summary>
    /// Load only the base entity without any related data
    /// Best performance for scenarios where related data is not needed
    /// </summary>
    None = 0,

    /// <summary>
    /// Include CertificateType navigation property
    /// Use when you need the certificate type name/details
    /// </summary>
    CertificateType = 1 << 0,

    /// <summary>
    /// Include DocumentBinding navigation property
    /// Use when you need binding information but not the full document
    /// </summary>
    DocumentBinding = 1 << 1,

    /// <summary>
    /// Include Document through DocumentBinding (requires DocumentBinding flag)
    /// Use when you need complete document information
    /// </summary>
    Document = 1 << 2,

    /// <summary>
    /// Include all navigation properties
    /// Convenience option for comprehensive data retrieval
    /// </summary>
    All = CertificateType | DocumentBinding | Document
}
