using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// API controller for managing Property Type Category master data.
/// Provides endpoints for CRUD operations on property type categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
 
public class PropertyTypeCategoryController : ControllerBase
{
    private readonly IPropertyTypeCategoryService _service;
    private readonly ILogger<PropertyTypeCategoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTypeCategoryController"/> class.
    /// </summary>
    /// <param name="service">The property type category service.</param>
    /// <param name="logger">The logger instance.</param>
    public PropertyTypeCategoryController(IPropertyTypeCategoryService service, ILogger<PropertyTypeCategoryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paged list of property type categories based on query parameters.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting, and paging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of property type categories.</returns>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyTypeCategoryQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Retrieves a property type category by its unique identifier.
    /// </summary>
    /// <param name="id">The property type category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The property type category if found; otherwise, NotFound.</returns>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Creates a new property type category.
    /// </summary>
    /// <param name="createDto">The property type category creation DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created property type category.</returns>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyTypeCategoryDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Updates an existing property type category.
    /// </summary>
    /// <param name="id">The property type category ID.</param>
    /// <param name="updateDto">The property type category update DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated property type category if found; otherwise, NotFound.</returns>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyTypeCategoryDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Deletes a property type category by its unique identifier.
    /// </summary>
    /// <param name="id">The property type category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted; otherwise, false.</returns>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
