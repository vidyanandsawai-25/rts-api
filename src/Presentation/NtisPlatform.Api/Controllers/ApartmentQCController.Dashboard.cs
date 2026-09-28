using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQC.ApartmentDashboard;
namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    [Authorize]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetApartmentDashboardDetailsAsync([FromQuery] ApartmentDashboardQueryParameters queryParameters, CancellationToken ct)
       => Ok(await _apartmentDashboardService.GetAllAsync(queryParameters, ct));
}
