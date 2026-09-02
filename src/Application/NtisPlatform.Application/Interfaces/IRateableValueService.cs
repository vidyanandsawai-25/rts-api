using System;
using System.Collections.Generic;
using System.Text;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.RateableValue;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces
{
    public interface IRateableValueService
    {
        /// <param name="propertyId">The property to calculate Rateable Value for.</param>
        /// <param name="forceRecalculate">
        /// When true, bypasses the input-signature fast path and always recalculates, even if
        /// nothing detectable has changed since the last run. Defaults to false.
        /// </param>
        Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId, bool forceRecalculate = false);
    }
}
