using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Dedicated controller to fetch TypeOfUse details by PropertyTypeId.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TypeOfUseByPropertyTypeController : ControllerBase
{
    private readonly ITypeOfUseByPropertyTypeService _service;
    private readonly ILogger<TypeOfUseByPropertyTypeController> _logger;

    public TypeOfUseByPropertyTypeController(
        ITypeOfUseByPropertyTypeService service,
        ILogger<TypeOfUseByPropertyTypeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);
}
