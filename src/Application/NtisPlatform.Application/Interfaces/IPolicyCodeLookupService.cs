namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Resolves PTIS.PolicyCodeMaster.PolicyCode string values (e.g. "NETTAX", "OC", "PARTIAL_CC")
/// to their Id, for services that tag PTIS.PolicyTaxDetails rows via PolicyCodeId. Shared by the
/// RV/CV pipeline (always "NETTAX") and the Occupation Tax engine (OC/CC/ELECTRIC_BILL family).
/// </summary>
public interface IPolicyCodeLookupService
{
    /// <summary>Resolves a single policy code. Throws if no active row exists for it.</summary>
    Task<int> GetIdAsync(string policyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves several policy codes in one query. Throws if any requested code has no active row.
    /// </summary>
    Task<Dictionary<string, int>> GetIdsAsync(IEnumerable<string> policyCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves whichever of the requested policy codes have an active row -- unlike
    /// <see cref="GetIdsAsync"/>, a code with no active row is simply absent from the result
    /// instead of throwing. Use when the caller can tolerate some requested codes being
    /// unconfigured (e.g. resolving several unrelated certificate-tax families at once, where one
    /// family being misconfigured shouldn't block the others).
    /// </summary>
    Task<Dictionary<string, int>> GetExistingIdsAsync(IEnumerable<string> policyCodes, CancellationToken cancellationToken = default);
}
