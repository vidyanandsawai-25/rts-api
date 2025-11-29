using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// API controller for Sample entity operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
// TODO: Add authorization attribute when authentication is configured
// [Authorize]
public class SampleController : ControllerBase
{
    private readonly ISampleService _sampleService;
    private readonly ILogger<SampleController> _logger;

    public SampleController(ISampleService sampleService, ILogger<SampleController> logger)
    {
        _sampleService = sampleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all samples
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SampleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SampleDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var samples = await _sampleService.GetAllAsync(cancellationToken);
            return Ok(samples);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all samples");
            return StatusCode(500, "An error occurred while retrieving samples");
        }
    }

    /// <summary>
    /// Get sample by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SampleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SampleDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var sample = await _sampleService.GetByIdAsync(id, cancellationToken);
            if (sample == null)
            {
                return NotFound($"Sample with ID {id} not found");
            }
            return Ok(sample);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sample {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the sample");
        }
    }

    /// <summary>
    /// Create a new sample
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SampleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SampleDto>> Create([FromBody] CreateSampleDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _sampleService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample");
            return StatusCode(500, "An error occurred while creating the sample");
        }
    }

    /// <summary>
    /// Update an existing sample
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSampleDto dto, CancellationToken cancellationToken)
    {
        if (id != dto.Id)
        {
            return BadRequest("ID mismatch");
        }

        try
        {
            await _sampleService.UpdateAsync(dto, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Sample not found for update: {Id}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sample {Id}", id);
            return StatusCode(500, "An error occurred while updating the sample");
        }
    }

    /// <summary>
    /// Delete a sample
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _sampleService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sample {Id}", id);
            return StatusCode(500, "An error occurred while deleting the sample");
        }
    }
}
