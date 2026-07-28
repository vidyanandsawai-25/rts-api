using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Current, tax-wise policy calculation state for a property. Shared by the Rateable Value
/// pipeline (PolicyCodeId = NETTAX) and the Occupation Tax engine (PolicyCodeId = OC/PARTIAL_OC/
/// CC/PARTIAL_CC/ELECTRIC_BILL/PARTIAL_ELECTRIC_BILL, same shape). DBA/lead/business-confirmed
/// final schema: exactly ONE active row per (PropertyId, PolicyCodeId, TaxId) -- no PolicyYear or
/// PolicyReason column exists on PTIS.PolicyTaxDetails; the table holds the CURRENT state only,
/// never a per-year history.
/// </summary>
public class PolicyTaxDetailsEntity : BaseEntity, IHardDeletable
{
    public virtual PropertyEntity? PropertyMast { get; set; }
    public virtual TaxMasterEntity? TaxMaster { get; set; }
    public virtual PolicyCodeMasterEntity? PolicyCodeMaster { get; set; }

    public int PropertyId { get; set; }

    public int PolicyCodeId { get; set; }

    public decimal? CalculationValue { get; set; }

    public int TaxId { get; set; }

    public decimal? TaxAmount { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
