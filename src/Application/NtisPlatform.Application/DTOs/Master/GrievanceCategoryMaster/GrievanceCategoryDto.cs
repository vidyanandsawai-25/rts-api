using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster
{
    public class GrievanceCategoryDto : CommonBaseDtos
    {
        public int Id { get; set; }
        public string CategoryCode { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string Priority { get; set; } = null!;
        public string? ResolutionSla { get; set; }
        public string? EscalationLevel { get; set; }
        public string? Description { get; set; }
    }
    
    public class CreateGrievanceCategoryDto : CreateCommonBaseDtos
    {
        [Required(ErrorMessage = "CategoryCode_Required")]
        [StringLength(50, ErrorMessage = "CategoryCode_MaxLen_50")]
        public string CategoryCode { get; set; } = null!;
        
        [Required(ErrorMessage = "CategoryName_Required")]
        [StringLength(200, ErrorMessage = "CategoryName_MaxLen_200")]
        public string CategoryName { get; set; } = null!;
        
        public int? DepartmentId { get; set; }
        
        [Required(ErrorMessage = "Priority_Required")]
        [StringLength(50, ErrorMessage = "Priority_MaxLen_50")]
        public string Priority { get; set; } = null!;
        
        [StringLength(100, ErrorMessage = "ResolutionSla_MaxLen_100")]
        public string? ResolutionSla { get; set; }
        
        [StringLength(100, ErrorMessage = "EscalationLevel_MaxLen_100")]
        public string? EscalationLevel { get; set; }
        
        [StringLength(1000, ErrorMessage = "Description_MaxLen_1000")]
        public string? Description { get; set; }
    }
    
    public class UpdateGrievanceCategoryDto : UpdateCommonBaseDtos
    {
        [Required(ErrorMessage = "CategoryCode_Required")]
        [StringLength(50, ErrorMessage = "CategoryCode_MaxLen_50")]
        public string CategoryCode { get; set; } = null!;
        [Required(ErrorMessage = "CategoryName_Required")]
        [StringLength(200, ErrorMessage = "CategoryName_MaxLen_200")]
        public string CategoryName { get; set; } = null!;
        public int? DepartmentId { get; set; }
        [Required(ErrorMessage = "Priority_Required")]
        [StringLength(50, ErrorMessage = "Priority_MaxLen_50")]
        public string Priority { get; set; } = null!;
        [StringLength(100, ErrorMessage = "ResolutionSla_MaxLen_100")]
        public string? ResolutionSla { get; set; }
        [StringLength(100, ErrorMessage = "EscalationLevel_MaxLen_100")]
        public string? EscalationLevel { get; set; }
        [StringLength(1000, ErrorMessage = "Description_MaxLen_1000")]
        public string? Description { get; set; }
    }
}
