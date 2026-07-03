using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;

namespace NtisPlatform.Application.DTOs.PropertyDetails
{
    public class PropertyDetailsDto : BaseDtos
    {
        public int? PropertyId { get; set; }
        public int? FloorId { get; set; }
        public string? FloorDescription { get; set; }
        public int? SubFloorId { get; set; }
        public string? SubFloorDescription { get; set; }
        public string? ConstructionYear { get; set; }
        public string? AssessmentYear { get; set; }
        public int? ConstructionTypeId { get; set; }
        public string? ConstructionTypeDescription { get; set; }
        public int? TypeOfUseId { get; set; }
        public string? TypeOfUseDescription { get; set; }
        public int? SubTypeOfUseId { get; set; }
        public string? SubTypeOfUseDescription { get; set; }
        public double? CarpetAreaSqFeet { get; set; }
        public double? CarpetAreaSqMeter { get; set; }
        public double? BuiltupAreaSqMeter { get; set; }
        public double? BuiltupAreaSqFeet { get; set; }
        public int? NoOfRooms { get; set; }
        public bool? IsRenter { get; set; }
        public bool? IsTaxable { get; set; }
        public bool? IsOpenPlot { get; set; }
        public double? Length { get; set; }
        public double? Width { get; set; }
        public List<RenterDetailDto>? RenterDetails { get; set; }
        public List<RenterMastDto>? Renters { get; set; }
        public List<RoomWiseSubmissionDetailsDto>? RoomWiseSubmissionDetails { get; set; }
        public PropertyDto? Property { get; set; }


    }
      
}
