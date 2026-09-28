using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC.ApartmentDashboard;

public class ApartmentDashboardQueryParameters
{
    [Required(ErrorMessage = "ApartmentDashboard_SocietyDetailsId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentDashboard_SocietyDetailsId_Invalid")]
    public int SocietyDetailsId { get; set; }
    public int WingId { get; set; } = 0;
}
