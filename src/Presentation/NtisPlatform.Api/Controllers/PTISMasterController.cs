using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PTISMasterController : ControllerBase
    {

        private readonly IPTISConstructionTypeMasterService _PTISMasterService;
        private readonly IPTISFloorMasterService _PTISFloorMasterService;
        private readonly ILogger<PTISMasterController> _logger;

        public PTISMasterController(IPTISConstructionTypeMasterService PTISMasterService, IPTISFloorMasterService PTISFloorMasterService,ILogger<PTISMasterController> logger)
        {
            _PTISMasterService = PTISMasterService;
            _PTISFloorMasterService = PTISFloorMasterService;
            _logger = logger;
        }

        [HttpGet("ConstructionTypeMasterGetAll")]
        [ProducesResponseType(typeof(IEnumerable<PTISConstructionTypeMasterEntity>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PTISConstructionTypeMasterEntity>>> ConstructionTypeMasterGetAll(CancellationToken cancellationToken)
        {
            try
            {
                var samples = await _PTISMasterService.ConstructionTypeMasterGetAllAsync(cancellationToken);
                return Ok(samples);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all data");
                return StatusCode(500, "An error occurred while retrieving data");
            }
        }


        [HttpPost("ConstructionTypeMasterCreate")]
        [ProducesResponseType(typeof(SampleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PTISConstructionTypeMasterDtoResponse>> ConstructionTypeMasterCreate([FromBody] PTISConstructionTypeMasterDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _PTISMasterService.ConstructionTypeMasterCreateAsync(dto, cancellationToken);
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data");
                return StatusCode(500, "An error occurred while creating the data");
            }
        }

        [HttpGet("ConstructionTypeMasterGetById{id}")]
        [ProducesResponseType(typeof(PTISConstructionTypeMasterEntity), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PTISConstructionTypeMasterEntity>> ConstructionTypeMasterGetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var sample = await _PTISMasterService.ConstructionTypeMasterGetByIdAsync(id, cancellationToken);
                if (sample == null)
                {
                    return NotFound($"data with ID {id} not found");
                }
                return Ok(sample);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data for {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the data");
            }
        }


        [HttpPut("ConstructionTypeMasterUpdate{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConstructionTypeMasterUpdate(int id, [FromBody] PTISConstructionTypeMasterDto dto, CancellationToken cancellationToken)
        {


            try
            {
                await _PTISMasterService.ConstructionTypeMasterUpdateAsync(id,dto, cancellationToken);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "data not found for update: {Id}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data {Id}", id);
                return StatusCode(500, "An error occurred while updating the data");
            }
        }

        [HttpDelete("ConstructionTypeMasterDelete{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConstructionTypeMasterDelete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _PTISMasterService.ConstructionTypeMasterDeleteAsync(id, cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting data {Id}", id);
                return StatusCode(500, "An error occurred while deleting the data");
            }
        }
		
		[HttpGet("GetAllFloorMaster")]
    [ProducesResponseType(typeof(IEnumerable<PTISFloorMasterEntity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PTISFloorMasterEntity>>> GetAllFloorMaster(CancellationToken cancellationToken)
    {
        try
        {
            var samples = await _PTISFloorMasterService.GetAllAsyncFloorMaster(cancellationToken);
            return Ok(samples);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all samples");
            return StatusCode(500, "An error occurred while retrieving samples");
        }
    }

    [HttpPost("CreateFloorMaster")]
    [ProducesResponseType(typeof(PTISFloorMasterEntity), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PTISFloorMasterEntity>> CreateFloorMaster([FromBody] PTISFloorMasterDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _PTISFloorMasterService.CreateAsyncFloorMaster(dto, cancellationToken);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample");
            return StatusCode(500, "An error occurred while creating the sample");
        }
    }

    [HttpGet("GetByIdFloorMaster{id}")]
    [ProducesResponseType(typeof(PTISFloorMasterEntity), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    
    public async Task<ActionResult<PTISFloorMasterEntity>> GetByIdFloorMaster(int id, CancellationToken cancellationToken)
    {
        try
        {
            var sample = await _PTISFloorMasterService.GetByIdAsyncFloorMaster(id, cancellationToken);
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

    [HttpPut("UpdateFloorMaster{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFloorMaster(int id, [FromBody] PTISFloorMasterDto dto, CancellationToken cancellationToken)
    {
        //if (id != dto.ID)
        //{
        //    return BadRequest("ID mismatch");
        //}

        try
        {
            await _PTISFloorMasterService.UpdateAsyncFloorMaster(id,dto, cancellationToken);
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

    [HttpDelete("DeleteFloorMaster{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFloorMaster(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _PTISFloorMasterService.DeleteAsyncFloorMaster(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sample {Id}", id);
            return StatusCode(500, "An error occurred while deleting the sample");
        }
    }
    }
	
	
}
