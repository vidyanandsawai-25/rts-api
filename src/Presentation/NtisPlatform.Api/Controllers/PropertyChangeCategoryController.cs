using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyChangeCategory;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertyChangeCategoryController : ControllerBase
{
    private readonly IPropertyChangeCategoryService _propertyChangeCategoryService;
    private readonly ILogger<PropertyChangeCategoryController> _logger;

    public PropertyChangeCategoryController(IPropertyChangeCategoryService propertyChangeCategoryService, ILogger<PropertyChangeCategoryController> logger)
    {
        _propertyChangeCategoryService = propertyChangeCategoryService;
        _logger = logger;
    }

    [HttpPut]
    public Task<IActionResult> PropertyChangeCategoryUpdateAsync([FromBody] UpdatePropertyChangeCategoryDto dto, CancellationToken cancellationToken = default)
        => this.ExecuteUpdate(_propertyChangeCategoryService, dto.PropertyId, dto, _logger, cancellationToken);

}
