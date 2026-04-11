using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    public class OfficeEntity : BaseEntity
    {        public string OfficeCode { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string City { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? EmailId { get; set; }
        public int? OfficeIncharge { get; set; }
        public int? DesignationMasterId { get; set; }
        public DateTime? EstablishedDate { get; set; }
    }
}
