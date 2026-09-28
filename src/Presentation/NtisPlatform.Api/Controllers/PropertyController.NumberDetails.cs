using Microsoft.AspNetCore.Mvc;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    [HttpGet("NumberDetails")]
    public async Task<IActionResult> GetPropertyNumberDetailsAsync(int propertyId, string filter, CancellationToken ct)
     => Ok(await _propertyNumberDetailsService.GetAsync(propertyId, filter, ct));
}
