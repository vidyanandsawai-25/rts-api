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
        Task<RateableValueResponseDto> CalculateAndSaveAsync(int propertyId);
    }
}
