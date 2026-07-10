using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.CitizenLoginDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[AllowAnonymous]

[Route("api/[controller]")]
[ApiController]
public class RTSCitizenLoginController : ControllerBase
{
    private readonly IRTSCitizenLoginService _RTSCitizenLoginDetailsService;
    private readonly ILogger<RTSCitizenLoginController> _logger;
    public RTSCitizenLoginController(IRTSCitizenLoginService RTSCitizenLoginDetailsService , ILogger<RTSCitizenLoginController> logger) 
    {
       _RTSCitizenLoginDetailsService = RTSCitizenLoginDetailsService;
        _logger = logger;
    }


    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyQueryParameters query, CancellationToken ct)
     => this.ExecuteGetAllPaged(_RTSCitizenLoginDetailsService, query, _logger, ct);

}
