
using NtisPlatform.Application.DTOs.RoomWiseMinusData;

namespace NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails
{
    public class UpdateRoomWiseSubmissionDetailsDto : UpdateBaseDtos
    {
        public int Id { get; set; }  // Required for identifying which record to update
        public int PropertyDetailsId { get; set; }
        public int? PropertyId { get; set; }
        public double? LengthMtr { get; set; }
        public double? WidthMtr { get; set; }
        public double? HeightMtr { get; set; }
        public double? AreaSqMtr { get; set; }
        public int? NoOfRooms { get; set; }
        public double? TotalAreaSqMtr { get; set; }
        public string? RoomNo { get; set; }
        public string? RoomType { get; set; }
        public string? Shape { get; set; }
        public bool OuterYesNo { get; set; } = false;
        public bool MinusYesNo { get; set; } = false;
        public string? SubmissionType { get; set; }
        public double? Base1Mtr { get; set; }
        public double? Base2Mtr { get; set; }
        public List<UpdateRoomWiseMinusDataDto>? RoomWiseMinusData { get; set; }
    }
}