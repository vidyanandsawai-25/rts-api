namespace NtisPlatform.Core.Constants;

/// <summary>
/// Well-known PropertyCertificateTypeMaster.CertificateTypeCode values consumed by the
/// Occupation Tax engine and the Building Permission floor-wise display. Matched
/// case-insensitively so seed-data casing differences don't break resolution.
/// </summary>
public static class CertificateTypeCodes
{
    public const string CC = "CC";
    public const string OC = "OC";
    public const string ElectricBill = "ELECTRIC_BILL";

    /// <summary>
    /// A change (usage change, plot-to-construction, or any other unrecorded change) an officer
    /// noticed during assessment with no other certificate evidence available. Consumed by the
    /// Retrospective Tax rule engine's "Change Detection" evidence type — add a
    /// PropertyCertificateTypeMaster row with this code (via the existing certificate-type admin
    /// API) to make it selectable on the Add Certificate Record screen; no schema change needed.
    /// </summary>
    public const string ChangeDetection = "CHANGE_DETECTION";
}
