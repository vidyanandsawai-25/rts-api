using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// DTO for PropertyTaxCalculationCVResults entity.
/// Note: Does not inherit from BaseDtos because the database uses BIGINT (long) for Id,
/// while BaseDtos uses int for Id.
/// </summary>
public class PropertyTaxCalculationCVResultsDto
{
    /// <summary>
    /// Primary key - uses long to match database BIGINT column type.
    /// </summary>
    public long Id { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public int PropertyDetailsId { get; set; }
    public int PropertyId { get; set; }
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? TaxPercentage { get; set; }
    public decimal? TaxAmount { get; set; }
    public int? RateCVMasterId { get; set; }
    public double? BaseValue { get; set; }

    // Actual rate amount from RateCVMaster
    public decimal? RateAmount { get; set; }

    // Factor IDs
    public int? FloorFactorCVId { get; set; }
    public int? AgeFactorCVId { get; set; }
    public int? NatureFactorCVId { get; set; }  // NTB Factor
    public int? UseFactorCVId { get; set; }

    // Actual factor values (computed from master tables when retrieved)
    public double? FloorFactor { get; set; }
    public double? AgeFactor { get; set; }
    public double? NTBFactor { get; set; }
    public double? UseFactor { get; set; }

    /// <summary>
    /// SHA256 hash of input fields used in CV calculation for change detection.
    /// </summary>
    public string? CVInputHash { get; set; }

    public bool? MarkedForDeletion { get; set; } = false;

    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreatePropertyTaxCalculationCVResultsDto : CreateBaseDtos
{
    public int PropertyDetailsId { get; set; }
    public int PropertyId { get; set; }
    public int TaxId { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? TaxPercentage { get; set; }
    public decimal? TaxAmount { get; set; }
    public int? RateCVMasterId { get; set; }
    public double? BaseValue { get; set; }

    // Factor IDs - store references instead of values
    public int? FloorFactorCVId { get; set; }
    public int? AgeFactorCVId { get; set; }
    public int? NatureFactorCVId { get; set; }  // NTB Factor
    public int? UseFactorCVId { get; set; }

    /// <summary>
    /// SHA256 hash of input fields used in CV calculation for change detection.
    /// </summary>
    public string? CVInputHash { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? CreatedDate { get; set; }
}

public class UpdatePropertyTaxCalculationCVResultsDto
{
    public decimal? CapitalValue { get; set; }
    public decimal? TaxPercentage { get; set; }
    public decimal? TaxAmount { get; set; }
    public int? RateCVMasterId { get; set; }
    public double? BaseValue { get; set; }

    // Factor IDs
    public int? FloorFactorCVId { get; set; }
    public int? AgeFactorCVId { get; set; }
    public int? NatureFactorCVId { get; set; }  // NTB Factor
    public int? UseFactorCVId { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedBy { get; set; }
}

public class PropertyTaxCalculationCVResultsQueryParameters : BaseQueryParameters
{
    public long? PropertyId { get; set; }
    public int? PropertyDetailsId { get; set; }
    public int? TaxId { get; set; }
    public bool? IsActive { get; set; }
}
