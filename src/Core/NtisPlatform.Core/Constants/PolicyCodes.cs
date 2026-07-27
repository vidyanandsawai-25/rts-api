namespace NtisPlatform.Core.Constants;

/// <summary>
/// Well-known PolicyCodeMaster.PolicyCode values consumed by the Occupation Tax engine.
/// OC/CC/ElectricBill intentionally share their string value with
/// <see cref="CertificateTypeCodes"/> since one certificate type maps onto one policy code
/// family. Each DATE_BASED family has two codes: the plain code tags a full finance-year
/// PolicyTaxDetails row, the PARTIAL_x code tags the prorated onset-year row (BR1/BR5) — chained
/// via PolicyCodeMaster.NextPolicyCodeId (e.g. OcPartial -> Oc). Business-final naming is
/// PARTIAL_CC/PARTIAL_OC/PARTIAL_ELECTRIC_BILL (the "PARTIAL_" prefix comes first); the older
/// CC_PARTIAL/OC_PARTIAL/ELECTRIC_PARTIAL naming is retired, not kept as an alias, since this
/// feature has not yet been deployed with seeded data under the old names.
/// </summary>
public static class PolicyCodes
{
    public const string Oc = CertificateTypeCodes.OC;
    public const string OcPartial = "PARTIAL_OC";

    public const string Cc = CertificateTypeCodes.CC;
    public const string CcPartial = "PARTIAL_CC";

    public const string ElectricBill = CertificateTypeCodes.ElectricBill;

    /// <summary>
    /// Electric Bill's partial-year code, also used for the no-certificate-fallback's prorated
    /// onset year, since that fallback is fed through the engine as an Electricity-Bill-condition
    /// date.
    /// </summary>
    public const string ElectricPartial = "PARTIAL_ELECTRIC_BILL";

    /// <summary>The Rateable Value pipeline's base annual tax policy (unrelated to certificates).</summary>
    public const string NetTax = "NETTAX";
}
