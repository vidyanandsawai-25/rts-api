using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class CreateBulkUpdateDefinitionFromSourceDto : IValidatableObject
{
    [Required(ErrorMessage = "BulkUpdateDefinition_UpdateName_Required")]
    [StringLength(200, ErrorMessage = "BulkUpdateDefinition_UpdateName_MaxLen_200")]
    public string UpdateName { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdateDefinition_TableId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "BulkUpdateDefinition_TableId_Range")]
    public int TableId { get; set; }

    public List<int> TableFieldIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TableFieldIds.Count == 0)
        {
            yield return new ValidationResult(
                "BulkUpdateDefinition_TableFieldIds_NonEmpty",
                new[] { nameof(TableFieldIds) }
            );
        }
        else if (TableFieldIds.Any(id => id <= 0))
        {
            yield return new ValidationResult(
                "BulkUpdateDefinition_TableFieldIds_Invalid",
                new[] { nameof(TableFieldIds) }
            );
        }
    }
}

public class BulkUpdateDefinitionResultDto
{
    public BulkUpdateMasterDto Master { get; set; } = new();
    public List<BulkUpdateFieldConfigDto> FieldConfigs { get; set; } = [];
}
