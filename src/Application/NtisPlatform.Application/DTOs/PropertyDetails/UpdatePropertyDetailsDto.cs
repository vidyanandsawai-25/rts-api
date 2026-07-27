using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;

namespace NtisPlatform.Application.DTOs.PropertyDetails
{
    public class UpdatePropertyDetailsDto : UpdateBaseDtos
    {
        [Required]
        public int PropertyId { get; set; }
        [Required(ErrorMessage = "FloorId_Required")]
        public int FloorId { get; set; }
        public string? FloorDescription { get; set; }
        public int? SubFloorId { get; set; }
        public string? SubFloorDescription { get; set; }

        [StringLength(4, ErrorMessage = "ConstructionYear_MaxLen_4")]
        public string? ConstructionYear { get; set; }

        [StringLength(4, ErrorMessage = "AssessmentYear_MaxLen_4")]
        public string? AssessmentYear { get; set; }
        public int? ConstructionTypeId { get; set; }
        public string? ConstructionTypeDescription { get; set; }
        public int? TypeOfUseId { get; set; }
        public string? TypeOfUseDescription { get; set; }
        public int? SubTypeOfUseId { get; set; }
        public string? SubTypeOfUseDescription { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "CarpetAreaSqFeet_Range_Min_0")]
        public double? CarpetAreaSqFeet { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "CarpetAreaSqMeter_Range_Min_0")]
        public double? CarpetAreaSqMeter { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "BuiltupAreaSqMeter_Range_Min_0")]
        public double? BuiltupAreaSqMeter { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "BuiltupAreaSqFeet_Range_Min_0")]
        public double? BuiltupAreaSqFeet { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "NoOfRooms_Range_Min_0")]
        public int? NoOfRooms { get; set; }
        public bool? IsRenter { get; set; }
        public bool? IsTaxable { get; set; }
        public bool? IsOpenPlot { get; set; }
        public List<UpdateRenterDetailsDto>? RenterDetails { get; set; }
        public List<UpdateRenterMastDto>? Renters { get; set; }
        public List<UpdateRoomWiseSubmissionDetailsDto>? RoomWiseSubmissionDetails { get; set; }
    }

}