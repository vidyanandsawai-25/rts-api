using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyBulkMerge;

public class UpdatePropertyBulkMergeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PropertyBulkMerge_PropertyIdList_Required")]
    [MinLength(1, ErrorMessage = "PropertyBulkMerge_PropertyIdList_MinOne")]
    public List<PropertyBulkMergeDetailsDto> PropertyIdList { get; set; } = new();
    public bool IsPreviousDataUpdate { get; set; } = true;
}

