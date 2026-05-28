using NtisPlatform.Application.DTOs.DualMethod;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Interfaces
{
    public interface IDualMethodService
    {
        Task<DualMethodDto> GetRVCVTaxesAsync(int propertyId, CancellationToken cancellationToken = default);

    }
}
