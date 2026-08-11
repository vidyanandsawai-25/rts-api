using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.TaxApplicability;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    /// <summary>
    /// API controller for managing tax applicability settings
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaxApplicabilityController : ControllerBase
    {
        private readonly ITaxApplicabilityService _service;
        private readonly ILogger<TaxApplicabilityController> _logger;

        public TaxApplicabilityController(
            ITaxApplicabilityService service,
            ILogger<TaxApplicabilityController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Gets tax applicability details based on property, financial year, type of use group, and calculation type
        /// </summary>
        /// <param name="query">Query parameters for tax applicability</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Paged result of tax applicability details</returns>
        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] TaxApplicabilityRequestDto query, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, query, _logger, ct);

        /// <summary>
        /// Creates new tax applicability settings for a property
        /// </summary>
        /// <param name="createDto">Tax applicability creation request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Created tax applicability response</returns>
        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateTaxApplicabilityRequestDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        /// <summary>
        /// Updates existing tax applicability settings for a property
        /// </summary>
        /// <param name="id">Tax applicability record ID</param>
        /// <param name="updateDto">Tax applicability update request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Updated tax applicability response</returns>
        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateTaxApplicabilityRequestDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        /// <summary>
        /// Gets property details mapped with unique finance years and type of use
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of property finance year and type of use details</returns>
        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetPropertyFinanceYearTypeOfUse(int propertyId, CancellationToken ct)
        {
            var result = await _service.GetPropertyFinanceYearTypeOfUseAsync(propertyId, ct);
            return Ok(result);
        }
    }
}
