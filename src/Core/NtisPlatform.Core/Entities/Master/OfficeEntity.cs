using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    public class OfficeEntity : CommonBaseEntity
    {
        public int Id { get; set; }
        [Column(TypeName = "nvarchar(50)")]
        public string OfficeCode { get; set; } = string.Empty;
        [Column(TypeName = "nvarchar(200)")]
        public string OfficeName { get; set; } = string.Empty;
        [Column(TypeName = "nvarchar(100)")]
        public string Type { get; set; } = string.Empty;
        [Column(TypeName = "nvarchar(500)")]
        public string? Address { get; set; }
        [Column(TypeName = "nvarchar(100)")]
        public string City { get; set; } = string.Empty;
        [Column(TypeName = "nvarchar(6)")]
        public string Pincode { get; set; } = string.Empty;
        [Column(TypeName = "nvarchar(20)")]
        public string? Phone { get; set; }
        [Column("email", TypeName = "nvarchar(200)")]
        public string? Email { get; set; }
        public int? OfficeIncharge { get; set; }
        public int? Designation { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? EstablishedDate { get; set; }
        public bool? Status { get; set; }
    }
}
