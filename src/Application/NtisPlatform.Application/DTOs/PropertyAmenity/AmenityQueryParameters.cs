using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyAmenity
{
    public class AmenityQueryParameters : BaseQueryParameters
    {
        [Required]
        public int WardId { get; set; }
        [Required]
        public int UserId { get; set; }
        public string? PropertyNo { get; set; }
        public string? PartitionNo { get; set; }
        public int? PropertyTypeId { get; set; }
    }
}
