using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyDashboard;

public class PropertyDashboardQueryParameters
{
    [Required(ErrorMessage = "PropertyDashboard_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyDashboard_UserId_Invalid")]
    public int UserId { get; set; }

    public int ZoneId { get; set; } = 0;

    public int WardId { get; set; } = 0;
}
