using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master
{
    /// <summary>
    /// Controller for Grievance Category Master CRUD operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    
    public class GrievanceCategoryController : ControllerBase
    {
        private readonly IGrievanceCategoryService _service;
        private readonly ILogger<GrievanceCategoryController> _logger;

        public GrievanceCategoryController(IGrievanceCategoryService service, ILogger<GrievanceCategoryController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Get all Grievance Categories with filtering, sorting, and pagination
        /// </summary>
        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] GrievanceCategoryQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        /// <summary>
        /// Get Grievance Category by ID
        /// </summary>
        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        /// <summary>
        /// Create new Grievance Category
        /// </summary>
        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateGrievanceCategoryDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        /// <summary>
        /// Update existing Grievance Category
        /// </summary>
        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateGrievanceCategoryDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        /// <summary>
        /// Delete Grievance Category
        /// </summary>
        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);
    }
}
