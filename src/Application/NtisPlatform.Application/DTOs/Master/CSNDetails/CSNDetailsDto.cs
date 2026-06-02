using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.CSNDetails
{
    // CSNDetails DTOs
    public class CSNDetailsDto : BaseDtos
    {
        public int RateCVMasterId { get; set; }
        public int MoujaId { get; set; }
        public string CSN { get; set; } = string.Empty;

    }

    public class CreateCSNDetailsDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "CSN_MoujaId_Required")]
        public int MoujaId { get; set; }

        [Required(ErrorMessage = "CSN_CSN_Required")]
        [StringLength(200, ErrorMessage = "CSN_CSN_MaxLen_200")]
        public string CSN { get; set; } = string.Empty;

        public int RateCVMasterId { get; set; }
    }

    public class UpdateCSNDetailsDto : UpdateBaseDtos
    {
        public int RateCVMasterId { get; set; }

        [Required(ErrorMessage = "CSN_MoujaId_Required")]
        public int MoujaId { get; set; }

        [Required(ErrorMessage = "CSN_CSN_Required")]
        [StringLength(200, ErrorMessage = "CSN_CSN_MaxLen_200")]
        public string CSN { get; set; } = string.Empty;
    }

}
