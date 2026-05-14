using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

public class PropertyMapMasterDtos : BaseDtos
{
    public int? ModuleId { get; set; }
    public int? ParentPropertyMapId { get; set; }
    public int VersionNo { get; set; }
    public string MappingCategory { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public string? Remark { get; set; }
}

public class CreatePropertyMapMasterDto : CreateBaseDtos
{
    public int? ModuleId { get; set; }
    
    public int? ParentPropertyMapId { get; set; }
    
    public int VersionNo { get; set; } = 1;
    
    [Required(ErrorMessage = "MappingCategory_Required")]
    [StringLength(30, ErrorMessage = "MappingCategory_MaxLen_30")]
    public string MappingCategory { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "ChangeReason_MaxLen_500")]
    public string? ChangeReason { get; set; }
    
    [StringLength(500, ErrorMessage = "Remark_MaxLen_500")]
    public string? Remark { get; set; }
}

public class UpdatePropertyMapMasterDto : UpdateBaseDtos
{
    public int? ModuleId { get; set; }
    
    public int? ParentPropertyMapId { get; set; }
    
    public int VersionNo { get; set; } = 1;
    
    [Required(ErrorMessage = "MappingCategory_Required")]
    [StringLength(30, ErrorMessage = "MappingCategory_MaxLen_30")]
    public string MappingCategory { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "ChangeReason_MaxLen_500")]
    public string? ChangeReason { get; set; }
    
    [StringLength(500, ErrorMessage = "Remark_MaxLen_500")]
    public string? Remark { get; set; }
}