namespace NtisPlatform.Core.Enums;

/// <summary>
/// Defines the single authoritative certificate issuance mode for an RTS service:
/// None (0), Digital DSC (1), or Manual Upload (2).
/// </summary>
public enum RTSCertificateType : byte
{
    /// <summary>
    /// प्रमाणपत्र आवश्यक नाही - No certificate is required or generated (Direct approval/completion).
    /// </summary>
    None = 0,

    /// <summary>
    /// डिजिटल सिस्टीम प्रमाणपत्र - System generates certificate dynamically from active CertificateTemplateMaster with DSC and QR.
    /// </summary>
    Digital = 1,

    /// <summary>
    /// विभागीय मॅन्युअल प्रमाणपत्र - Physical certificate prepared by department; officer uploads file (PDF/Image) with statutory collection notice.
    /// </summary>
    Manual = 2
}
