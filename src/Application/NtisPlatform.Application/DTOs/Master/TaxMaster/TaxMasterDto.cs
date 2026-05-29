using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class TaxMasterDto : BaseDtos
{
    public string? TaxCode { get; set; }
    public string? TaxName { get; set; }
    public string? TaxNameAlias { get; set; }
    public int TaxCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool TaxOnUnit { get; set; }
    public bool AssessmentStatus { get; set; }
    public bool OldTaxStatus { get; set; }
}

public class CreateTaxMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "TaxMaster_TaxCode_Required")]
    [StringLength(20, ErrorMessage = "TaxMaster_TaxCode_MaxLengthExceeded_20")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "TaxMaster_TaxCode_Invalid")]
    public string? TaxCode { get; set; }

    [Required(ErrorMessage = "TaxMaster_TaxName_Required")]
    [StringLength(200, ErrorMessage = "TaxMaster_TaxName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "TaxMaster_TaxName_Invalid")]
    public string? TaxName { get; set; }

    [StringLength(200, ErrorMessage = "TaxMaster_TaxNameAlias_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "TaxMaster_TaxNameAlias_Invalid")]
    public string? TaxNameAlias { get; set; }

    [Required(ErrorMessage = "TaxMaster_TaxCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "TaxMaster_TaxCategoryId_MustBePositive")]
    public int TaxCategoryId { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool TaxOnUnit { get; set; } = false;

    public bool AssessmentStatus { get; set; } = true;

    public bool OldTaxStatus { get; set; } = true;
}

public class UpdateTaxMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TaxMaster_TaxCode_Required")]
    [StringLength(20, ErrorMessage = "TaxMaster_TaxCode_MaxLengthExceeded_20")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "TaxMaster_TaxCode_Invalid")]
    public string? TaxCode { get; set; }

    [Required(ErrorMessage = "TaxMaster_TaxName_Required")]
    [StringLength(200, ErrorMessage = "TaxMaster_TaxName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "TaxMaster_TaxName_Invalid")]
    public string? TaxName { get; set; }

    [StringLength(200, ErrorMessage = "TaxMaster_TaxNameAlias_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "TaxMaster_TaxNameAlias_Invalid")]
    public string? TaxNameAlias { get; set; }

    [Required(ErrorMessage = "TaxMaster_TaxCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "TaxMaster_TaxCategoryId_MustBePositive")]
    public int TaxCategoryId { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool TaxOnUnit { get; set; } = false;

    public bool AssessmentStatus { get; set; } = true;

    public bool OldTaxStatus { get; set; } = true;
}
