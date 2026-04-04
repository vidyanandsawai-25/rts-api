using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// API controller for managing Property Category master data.
/// Provides endpoints for CRUD operations on property categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyCategoryController : ControllerBase
{
    private readonly IPropertyCategoryService _service;
    private readonly ILogger<PropertyCategoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyCategoryController"/> class.
    /// </summary>
    /// <param name="service">The property category service.</param>
    /// <param name="logger">The logger instance.</param>
    public PropertyCategoryController(IPropertyCategoryService service, ILogger<PropertyCategoryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paged list of property categories based on query parameters.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting, and paging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of property categories.</returns>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyCategoryQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Retrieves a property category by its unique identifier.
    /// </summary>
    /// <param name="id">The property category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The property category if found; otherwise, NotFound.</returns>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Creates a new property category.
    /// </summary>
    /// <param name="createDto">The property category creation DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created property category.</returns>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] PropertyCategoryCreateDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Updates an existing property category.
    /// </summary>
    /// <param name="id">The property category ID.</param>
    /// <param name="updateDto">The property category update DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated property category if found; otherwise, NotFound.</returns>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] PropertyCategoryUpdateDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Deletes a property category by its unique identifier.
    /// </summary>
    /// <param name="id">The property category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted; otherwise, false.</returns>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}