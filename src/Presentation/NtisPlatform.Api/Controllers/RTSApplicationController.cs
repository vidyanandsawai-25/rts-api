using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class RTSApplicationController : ControllerBase
    {
        private readonly IRTSApplicationService _service;
        private readonly ILogger<RTSApplicationController> _logger;

        public RTSApplicationController(IRTSApplicationService service,ILogger<RTSApplicationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRTSApplicationDetailsDto dto, CancellationToken ct)
            => this.ExecuteCreate(_service, dto, _logger, ct);

}
