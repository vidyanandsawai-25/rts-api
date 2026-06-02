using NtisPlatform.Application.DTOs.CapitalValue;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService
{
    public interface ICapitalValueService
    {

        Task<List<CapitalValueDto>> CreateAsync(CreateCapitalValueDto dto, CancellationToken cancellationToken = default);
        Task<List<CapitalValueDto>> GetAsync(int propertyId, CancellationToken cancellationToken = default);
 
    }
}
