using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class ScreenFormSectionMasterDto : BaseDtos
{
    public int ScreenId { get; set; }
    public int? ParentSectionId { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string? SectionNameLocal { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int ColumnCount { get; set; }
    public bool IsOptional { get; set; }
    public bool IsCollapsible { get; set; }
    public bool IsCollapsedByDefault { get; set; }
    public bool IsRepeatable { get; set; }
}

public class CreateScreenFormSectionMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ScreenFormSectionMaster_ScreenId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormSectionMaster_ScreenId_Required")]
    public int ScreenId { get; set; }
    public int? ParentSectionId { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_SectionType_Required")]
    [StringLength(50, ErrorMessage = "ScreenFormSectionMaster_SectionType_MaxLen_50")]
    public string SectionType { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormSectionMaster_SectionName_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormSectionMaster_SectionName_MaxLen_200")]
    public string SectionName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenFormSectionMaster_SectionNameLocal_MaxLen_200")]
    public string? SectionNameLocal { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_SectionCode_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormSectionMaster_SectionCode_MaxLen_200")]
    public string SectionCode { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "ScreenFormSectionMaster_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_ColumnCount_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormSectionMaster_ColumnCount_Range")]
    public int? ColumnCount { get; set; } = 2;
    public bool? IsOptional { get; set; } = false;
    public bool? IsCollapsible { get; set; } = false;
    public bool? IsCollapsedByDefault { get; set; } = false;
    public bool? IsRepeatable { get; set; } = false;
}

public class UpdateScreenFormSectionMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ScreenFormSectionMaster_ScreenId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormSectionMaster_ScreenId_Required")]
    public int ScreenId { get; set; }
    public int? ParentSectionId { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_SectionType_Required")]
    [StringLength(50, ErrorMessage = "ScreenFormSectionMaster_SectionType_MaxLen_50")]
    public string SectionType { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScreenFormSectionMaster_SectionName_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormSectionMaster_SectionName_MaxLen_200")]
    public string SectionName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "ScreenFormSectionMaster_SectionNameLocal_MaxLen_200")]
    public string? SectionNameLocal { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_SectionCode_Required")]
    [StringLength(200, ErrorMessage = "ScreenFormSectionMaster_SectionCode_MaxLen_200")]
    public string SectionCode { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "ScreenFormSectionMaster_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }

    [Required(ErrorMessage = "ScreenFormSectionMaster_ColumnCount_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ScreenFormSectionMaster_ColumnCount_Range")]
    public int? ColumnCount { get; set; }
    public bool IsOptional { get; set; }
    public bool IsCollapsible { get; set; }
    public bool IsCollapsedByDefault { get; set; }
    public bool IsRepeatable { get; set; }
}