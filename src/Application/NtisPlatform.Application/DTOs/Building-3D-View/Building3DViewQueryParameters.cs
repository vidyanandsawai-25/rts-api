using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Building3DView
{
    public class Building3DViewQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid PropertyId is required")]
        public int PropertyId { get; set; }

        [Filterable]
        [Sortable]
        public int? WingdetailsId { get; set; }
    }
}
