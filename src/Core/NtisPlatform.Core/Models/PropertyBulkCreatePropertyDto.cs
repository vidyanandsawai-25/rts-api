using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models
{
    public class CreateBulkPropertyResponseDto
    {
        public int PropertyId { get; set; }
        public string? Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
    }
    public class CreateBulkPropertyDto
    {
        // -- Property basic details ---------------------------------------------

        [Required(ErrorMessage = "CreateBulkProperty_TaxZoneId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateBulkProperty_TaxZoneId_RangeMax")]
        public int TaxZoneId { get; set; }

        [Required(ErrorMessage = "CreateBulkProperty_WardId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateBulkProperty_WardId_RangeMax")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "CreateBulkProperty_PropertyNo_Required")]
        [StringLength(50, ErrorMessage = "CreateBulkProperty_PropertyNo_MaxLength")]
        public required string PropertyNo { get; set; }

        [Required(ErrorMessage = "CreateBulkProperty_PropertyTypeId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateBulkProperty_PropertyTypeId_RangeMax")]
        public int PropertyTypeId { get; set; }

        [Required(ErrorMessage = "CreateBulkProperty_CategoryId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CreateBulkProperty_CategoryId_RangeMax")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "CreateBulkProperty_PartitionNo_Required")]
        [StringLength(10, ErrorMessage = "CreateBulkProperty_PartitionNo_MaxLength")]
        public string PartitionNo { get; set; }=string.Empty;



        // -- Flat / shop details ------------------------------------------------

        [StringLength(100, ErrorMessage = "CreateBulkProperty_FlatOrShopNo_MaxLength")]
        public string? FlatOrShopNo { get; set; }

        [StringLength(100, ErrorMessage = "CreateBulkProperty_FlatOrShopNoEnglish_MaxLength")]
        public string? FlatOrShopNoEnglish { get; set; }
        // -- Address details ----------------------------------------------------

        [StringLength(500, ErrorMessage = "CreateBulkProperty_Address_MaxLength")]
        public string? Address { get; set; }

        [StringLength(500, ErrorMessage = "CreateBulkProperty_AddressEnglish_MaxLength")]
        public string? AddressEnglish { get; set; }

        [StringLength(200, ErrorMessage = "CreateBulkProperty_Location_MaxLength")]
        public string? Location { get; set; }

        [StringLength(200, ErrorMessage = "CreateBulkProperty_LocationEnglish_MaxLength")]
        public string? LocationEnglish { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateBulkProperty_SocietyDetailId_RangeMax")]
        public int? SocietyDetailId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CreateBulkProperty_CreatedBy_RangeMax")]
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
