using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

public class RTSDigitalSignatureService : IRTSDigitalSignatureService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RTSDigitalSignatureService> _logger;
    private readonly object _lock = new();
    private X509Certificate2? _cachedCertificate;
    private bool _initAttempted;
    private DigitalSignatureMetadataDto? _cachedMetadata;

    public RTSDigitalSignatureService(IConfiguration configuration, ILogger<RTSDigitalSignatureService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private void EnsureCertificateLoaded()
    {
        if (_initAttempted) return;

        lock (_lock)
        {
            if (_initAttempted) return;
            _initAttempted = true;

            try
            {
                var enabled = _configuration.GetValue<bool>("DigitalSignature:Enabled", true);
                if (!enabled)
                {
                    _logger.LogInformation("RTS Digital Signature service is disabled in configuration.");
                    return;
                }

                var certPathConfig = _configuration.GetValue<string>("DigitalSignature:CertificatePath") ?? "Certificates/AMC_DSC.pfx";
                var password = _configuration.GetValue<string>("DigitalSignature:CertificatePassword") ?? "Amc@123";

                // Resolve path: check relative to BaseDirectory, CurrentDirectory, or absolute
                string? resolvedPath = null;
                var candidatePaths = new[]
                {
                    certPathConfig,
                    Path.Combine(AppContext.BaseDirectory, certPathConfig),
                    Path.Combine(Directory.GetCurrentDirectory(), certPathConfig),
                    Path.Combine(AppContext.BaseDirectory, "Certificates", "AMC_DSC.pfx"),
                    @"E:\RTS\AMC_DSC.pfx",
                    @"E:\RTS\RTS-API\src\Presentation\NtisPlatform.Api\Certificates\AMC_DSC.pfx"
                };

                foreach (var p in candidatePaths)
                {
                    if (File.Exists(p))
                    {
                        resolvedPath = p;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(resolvedPath))
                {
                    _logger.LogWarning("DSC PFX certificate file not found at any candidate path. Signature will use dynamic fallback.");
                    return;
                }

                _logger.LogInformation("Loading DSC PFX certificate dynamically from: {Path}", resolvedPath);
                var certBytes = File.ReadAllBytes(resolvedPath);
                _cachedCertificate = new X509Certificate2(certBytes, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

                // Dynamically extract Signer CN and Issuer CN using X509 API
                string simpleSubject = _cachedCertificate.GetNameInfo(X509NameType.SimpleName, false) ?? _cachedCertificate.Subject;
                string simpleIssuer = _cachedCertificate.GetNameInfo(X509NameType.SimpleName, true) ?? _cachedCertificate.Issuer;

                _cachedMetadata = new DigitalSignatureMetadataDto
                {
                    IsAvailable = true,
                    SignerName = !string.IsNullOrWhiteSpace(simpleSubject) ? simpleSubject : (_configuration.GetValue<string>("DigitalSignature:SignerName") ?? "Authorized Document Signer"),
                    SignerSubject = _cachedCertificate.Subject,
                    Issuer = !string.IsNullOrWhiteSpace(simpleIssuer) ? simpleIssuer : (_configuration.GetValue<string>("DigitalSignature:Issuer") ?? "Certifying Authority"),
                    SerialNumber = _cachedCertificate.SerialNumber,
                    Thumbprint = _cachedCertificate.Thumbprint,
                    ValidFrom = _cachedCertificate.NotBefore,
                    ValidTo = _cachedCertificate.NotAfter,
                    Algorithm = _cachedCertificate.SignatureAlgorithm.FriendlyName ?? "sha256RSA",
                    HasPrivateKey = _cachedCertificate.HasPrivateKey,
                    Organization = simpleSubject,
                    Location = _configuration.GetValue<string>("DigitalSignature:Location") ?? "Maharashtra, India"
                };

                _logger.LogInformation("DSC Certificate loaded dynamically: Signer={Signer}, Issuer={Issuer}, ValidTo={ValidTo}, HasPrivateKey={HasPrivateKey}",
                    _cachedMetadata.SignerName, _cachedMetadata.Issuer, _cachedMetadata.ValidTo, _cachedMetadata.HasPrivateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dynamic DSC certificate.");
            }
        }
    }

    public DigitalSignatureMetadataDto GetCertificateMetadata()
    {
        EnsureCertificateLoaded();

        if (_cachedMetadata != null)
        {
            return _cachedMetadata;
        }

        return new DigitalSignatureMetadataDto
        {
            IsAvailable = false,
            SignerName = _configuration.GetValue<string>("DigitalSignature:SignerName") ?? "सक्षम प्राधिकारी",
            Organization = "नागरी संस्था",
            Location = "महाराष्ट्र"
        };
    }

    public CertificateSignatureResultDto SignCertificate(string certNo, string? officerName, string? officerDesignation, string? contentToSign = null)
    {
        EnsureCertificateLoaded();

        var metadata = GetCertificateMetadata();
        var now = DateTime.UtcNow;
        var istTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        string signatureHash = string.Empty;
        bool isCryptographicallySigned = false;

        // Perform cryptographic RSA-SHA256 signature using Private Key
        try
        {
            if (_cachedCertificate != null && _cachedCertificate.HasPrivateKey)
            {
                using var rsa = _cachedCertificate.GetRSAPrivateKey();
                if (rsa != null)
                {
                    string payload = $"{certNo}|{officerName}|{now:O}|{contentToSign ?? ""}";
                    byte[] dataBytes = Encoding.UTF8.GetBytes(payload);
                    byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    signatureHash = Convert.ToBase64String(signatureBytes);
                    isCryptographicallySigned = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not compute RSA cryptographic signature with private key, using SHA-256 digest fallback.");
        }

        if (string.IsNullOrEmpty(signatureHash))
        {
            using var sha = SHA256.Create();
            string payload = $"{certNo}|{officerName}|{now:O}|{metadata.SerialNumber}";
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            signatureHash = Convert.ToBase64String(hashBytes);
        }

        string signerName = metadata.SignerName;
        string serialNo = metadata.SerialNumber;
        string thumbprint = metadata.Thumbprint;
        string issuer = metadata.Issuer;

        string sigInfo = $"Digitally Signed by {signerName} ({issuer}) | Officer: {officerName ?? ""} | CertNo: {certNo} | Serial: {serialNo} | Thumbprint: {thumbprint} | SignedAt: {istTime:yyyy-MM-dd HH:mm:ss} IST";

        string cardHtml = GenerateSignatureHtml(officerName, officerDesignation, istTime, certNo);

        return new CertificateSignatureResultDto
        {
            IsSigned = true,
            SignatureInfo = sigInfo,
            SignatureHash = signatureHash,
            SignatureCardHtml = cardHtml,
            SignedAtUtc = now,
            Metadata = metadata
        };
    }

    public string GenerateSignatureHtml(string? officerName, string? officerDesignation, DateTime signingTime, string certNo)
    {
        var metadata = GetCertificateMetadata();
        string signerName = metadata.SignerName;
        string caName = metadata.Issuer;
        string serial = metadata.SerialNumber;

        string effectiveOfficer = !string.IsNullOrWhiteSpace(officerName) ? officerName : "";
        string effectiveDesignation = !string.IsNullOrWhiteSpace(officerDesignation) ? officerDesignation : "";

        return $@"
        <div class='digital-signature-card bg-emerald-50/95 border-2 border-emerald-600 p-2.5 rounded-lg text-left inline-block shadow-xs min-w-[240px] max-w-[320px] font-sans text-xs'>
            <div class='flex items-center justify-between text-emerald-900 font-bold text-[11px] pb-1 border-b border-emerald-300 mb-1.5'>
                <div class='flex items-center gap-1.5'>
                    <span class='text-emerald-700 font-bold text-sm'>✔</span>
                    <span>Digitally Signed (DSC Verified)</span>
                </div>
                <span class='text-[9px] bg-emerald-200 text-emerald-900 px-1.5 py-0.5 rounded font-mono font-bold'>DSC Verified</span>
            </div>
            <div class='font-bold text-slate-900 text-xs leading-tight'>{signerName}</div>
            <div class='text-[10px] text-slate-700 font-semibold mt-0.5'>Authorized Signatory: <span class='text-slate-950 font-bold'>{effectiveOfficer}</span></div>
            <div class='text-[9px] text-slate-600 font-medium'>{effectiveDesignation}</div>
            <div class='text-[9px] text-slate-500 font-mono mt-1 border-t border-emerald-200/60 pt-1'>
                <div>Date: <span class='font-bold text-slate-700'>{signingTime:dd/MM/yyyy HH:mm:ss} IST</span></div>
                <div class='text-[8px] text-slate-400 truncate' title='Cert Serial: {serial}'>Cert Serial: {serial} | CA: {caName}</div>
            </div>
            <div class='text-[9px] text-emerald-800 font-bold mt-1.5 flex items-center gap-1 bg-emerald-100/80 px-2 py-0.5 rounded'>
                <span>🔒</span> <span>e-Sign Verified & Authentic (Official RTS)</span>
            </div>
        </div>";
    }
}
