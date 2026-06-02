using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class PolicyTaxDetailsDto : BaseDtos
{
    public int PropertyId { get; set; }
    public string? PolicyCode { get; set; }
    public DateTime? PolicyDate { get; set; }
    public short? PolicyYear { get; set; }
    public string? PolicyReason { get; set; }
    public decimal? PolicyRVorCVvalue { get; set; }
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public decimal? TaxAmount { get; set; }
    public bool MarkedForDeletion { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreatePolicyTaxDetailsDto : CreateBaseDtos
{
    public int PropertyId { get; set; }
    public string? PolicyCode { get; set; }
    public DateTime? PolicyDate { get; set; }
    public short? PolicyYear { get; set; }
    public string? PolicyReason { get; set; }
    public decimal? PolicyRVorCVvalue { get; set; }
    public int TaxId { get; set; }
    public decimal? TaxAmount { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class UpdatePolicyTaxDetailsDto : UpdateBaseDtos
{
    public string? PolicyCode { get; set; }
    public DateTime? PolicyDate { get; set; }
    public short? PolicyYear { get; set; }
    public string? PolicyReason { get; set; }
    public decimal? PolicyRVorCVvalue { get; set; }
    public decimal? TaxAmount { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class PolicyTaxDetailsQueryParameters : BaseQueryParameters
{
    public int? PropertyId { get; set; }
    public int? TaxId { get; set; }
    public short? PolicyYear { get; set; }
    public bool? IsActive { get; set; }
}
