using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.YearMaster
{
    public class YearMasterDto
    {
        public int? YearId { get; set; }
        public int? Year { get; set; }
        public string? YearCode { get; set; }
        public bool IsActive { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
    
    public class CreateYearMasterDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "Year_Required")]
        [Range(1900, 2100, ErrorMessage = "Year_Range")]
        public int Year { get; set; }
        
        [Required(ErrorMessage = "YearCode_Required")]
        [StringLength(20, ErrorMessage = "YearCode_MaxLen_20")]
        public string YearCode { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "YearStatus_MaxLen_50")]
        public string? Status { get; set; }
        
        [Required(ErrorMessage = "StartDate_Required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "EndDate_Required")]
        public DateTime EndDate { get; set; }
        
        [StringLength(250, ErrorMessage = "YearDescription_MaxLen_250")]
        public string? Description { get; set; }
    }
    
    public class UpdateYearMasterDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "Year_Required")]
        [Range(1900, 2100, ErrorMessage = "Year_Range")]
        public int Year { get; set; }
        
        [Required(ErrorMessage = "YearCode_Required")]
        [StringLength(20, ErrorMessage = "YearCode_MaxLen_20")]
        public string YearCode { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "YearStatus_MaxLen_50")]
        public string? Status { get; set; }
        
        [Required(ErrorMessage = "StartDate_Required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "EndDate_Required")]
        public DateTime EndDate { get; set; }
        
        [StringLength(250, ErrorMessage = "YearDescription_MaxLen_250")]
        public string? Description { get; set; }
    }
}
