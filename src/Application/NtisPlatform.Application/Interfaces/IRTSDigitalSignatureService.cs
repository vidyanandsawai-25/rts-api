using System;

namespace NtisPlatform.Application.Interfaces;

public class DigitalSignatureMetadataDto
{
    public bool IsAvailable { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerSubject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public bool HasPrivateKey { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class CertificateSignatureResultDto
{
    public bool IsSigned { get; set; }
    public string SignatureInfo { get; set; } = string.Empty;
    public string SignatureHash { get; set; } = string.Empty;
    public string SignatureCardHtml { get; set; } = string.Empty;
    public DateTime SignedAtUtc { get; set; } = DateTime.UtcNow;
    public DigitalSignatureMetadataDto? Metadata { get; set; }
}

public interface IRTSDigitalSignatureService
{
    DigitalSignatureMetadataDto GetCertificateMetadata();
    CertificateSignatureResultDto SignCertificate(string certNo, string? officerName, string? officerDesignation, string? contentToSign = null);
    string GenerateSignatureHtml(string? officerName, string? officerDesignation, DateTime signingTime, string certNo);
}
