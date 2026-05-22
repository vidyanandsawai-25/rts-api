using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities
{
    [Table("PropertyOccupancyDetails", Schema = "PTIS")]
    public class PropertyOccupancyDetailsEntity :BaseEntity 
    {
      
        public int PropertyDetailId { get; set; }
        public DateTime? OccupancyDate { get; set; }
        [Column(TypeName = "nvarchar(30)")]
        public string? OccupancyNumber { get; set; }
        [Column(TypeName = "nvarchar(100)")]
        public string? IssuedBy { get; set; }
        [Column(TypeName = "nvarchar(250)")]
        public string? Remarks { get; set; }
        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
     
    }
}
