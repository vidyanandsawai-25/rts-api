using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RoomWiseMinusData
{
    public class CreateRoomWiseMinusDataDto : CreateBaseDtos
    {

        public int RoomWiseSubmissionId { get; set; }    // set explicitly in service

        [Range(0, double.MaxValue, ErrorMessage = "LengthMtr_Range_Min_0")]
        public double? LengthMtr { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "WidthMtr_Range_Min_0")]
        public double? WidthMtr { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "HeightMtr_Range_Min_0")]
        public double? HeightMtr { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "AreaSqMtr_Range_Min_0")]
        public double? AreaSqMtr { get; set; }

        [StringLength(20, ErrorMessage = "Shape_MaxLen_20")]
        public string? Shape { get; set; }

        public double? Base1Mtr { get; set; }
        public double? Base2Mtr { get; set; }
    }
}