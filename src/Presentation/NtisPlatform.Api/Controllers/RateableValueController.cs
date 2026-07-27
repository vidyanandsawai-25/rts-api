using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/rateable-value")]
    [Authorize]
    public class RateableValueController : ControllerBase
    {
        private readonly IRateableValueService _rateableValueService;
        private readonly IOccupationTaxService _occupationTaxService;
        private readonly ILogger<RateableValueController> _logger;

        public RateableValueController(
            IRateableValueService rateableValueService,
            IOccupationTaxService occupationTaxService,
            ILogger<RateableValueController> logger)
        {
            _rateableValueService = rateableValueService;
            _occupationTaxService = occupationTaxService;
            _logger = logger;
        }

        [HttpPost("{propertyId:int}")]
        public async Task<ActionResult<RateableValueResponseDto>> Calculate(int propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("propertyId must be a positive integer.");

            try
            {
                var result = await _rateableValueService.CalculateAndSaveAsync(propertyId);

                // This standalone RV recalculation endpoint is the only real-recalculation path that
                // never otherwise reaches OccupationTaxApplicationService -- unlike the
                // certificate-change pipeline (PropertyCertificateChangedEventHandler), which already
                // calls RV-refresh-then-Occupation-Tax-apply in strict order on its own, so adding
                // this call there too would double-run it. Without this, CC/OC/Electric-Bill amounts
                // (which read NETTAX rate snapshots RV just wrote) go stale until the next
                // certificate-change event happens to fire. A failure here must not fail the RV
                // response that already succeeded -- log and continue.
                try
                {
                    await _occupationTaxService.ApplyAsync(propertyId, GetUserId());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply Occupation Tax after standalone RV recalculation for PropertyId={PropertyId}", propertyId);
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "RV calculation rejected for PropertyId={PropertyId}", propertyId);
                return NotFound(new { error = "Rateable value calculation could not be completed for the requested property." });
            }
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            {
                throw new UnauthorizedAccessException("Valid user identification is required.");
            }
            return id;
        }
    }
}
