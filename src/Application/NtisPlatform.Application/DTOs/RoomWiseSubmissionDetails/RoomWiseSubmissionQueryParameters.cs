using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails
{
    public class RoomWiseSubmissionQueryParameters : BaseQueryParameters
    {
        [Required(ErrorMessage = "RoomWiseSubmission_PropertyDetailsId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "RoomWiseSubmission_PropertyDetailsId_Range_Min_1")]
        public int PropertyDetailsId { get; set; }
        public int? PropertyId { get; set; }
    }
}
