using NtisPlatform.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.BankMaster
{
    public class BankMasterDTO
    {
        public int Id { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string IFSCCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateBankMasterDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "BankCode_Required")]
        [StringLength(50, ErrorMessage = "BankCode_MaxLen_50")]
        public string BankCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "BankName_Required")]
        [StringLength(200, ErrorMessage = "BankName_MaxLen_200")]
        public string BankName { get; set; } = string.Empty;
        [Required(ErrorMessage = "BranchName_Required")]
        [StringLength(200, ErrorMessage = "BranchName_MaxLen_200")]
        public string BranchName { get; set; } = string.Empty;
        [Required(ErrorMessage = "IFSCCode_Required")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "IFSCCode_Length_11")]
        [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "IFSCCode_Invalid_Format")]
        public string IFSCCode { get; set; } = string.Empty;
        [StringLength(500, ErrorMessage = "Address_MaxLen_500")]
        public string Address { get; set; } = string.Empty;
        [Required(ErrorMessage = "City_Required")]
        [StringLength(100, ErrorMessage = "City_MaxLen_100")]
        public string City { get; set; } = string.Empty;
        [Required(ErrorMessage = "State_Required")]
        [StringLength(100, ErrorMessage = "State_MaxLen_100")]
        public string State { get; set; } = string.Empty;
        [Required(ErrorMessage = "Pincode_Required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode_Length_6")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode_Invalid_Format")]
        public string Pincode { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "Status_MaxLen_50")]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateBankMasterDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "BankCode_Required")]
        [StringLength(50, ErrorMessage = "BankCode_MaxLen_50")]
        public string BankCode { get; set; } = string.Empty;
        [Required(ErrorMessage = "BankName_Required")]
        [StringLength(200, ErrorMessage = "BankName_MaxLen_200")]
        public string BankName { get; set; } = string.Empty;
        [Required(ErrorMessage = "BranchName_Required")]
        [StringLength(200, ErrorMessage = "BranchName_MaxLen_200")]
        public string BranchName { get; set; } = string.Empty;
        [Required(ErrorMessage = "IFSCCode_Required")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "IFSCCode_Length_11")]
        [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "IFSCCode_Invalid_Format")]
        public string IFSCCode { get; set; } = string.Empty;
        [StringLength(500, ErrorMessage = "Address_MaxLen_500")]
        public string Address { get; set; } = string.Empty;
        [Required(ErrorMessage = "City_Required")]
        [StringLength(100, ErrorMessage = "City_MaxLen_100")]
        public string City { get; set; } = string.Empty;
        [Required(ErrorMessage = "State_Required")]
        [StringLength(100, ErrorMessage = "State_MaxLen_100")]
        public string State { get; set; } = string.Empty;
        [Required(ErrorMessage = "Pincode_Required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode_Length_6")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode_Invalid_Format")]
        public string Pincode { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "Status_MaxLen_50")]
        public string Status { get; set; } = string.Empty;
    }
}
