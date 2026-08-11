using NtisPlatform.Application.DTOs.PropertyComparison;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyComparisonService
{
    /// <summary>
    /// Compare old and new property data based on property mapping
    /// </summary>
    /// <param name="newPropertyId">The new property ID (old property ID is fetched from PropertyMapDetail)</param>
    /// <returns>Property comparison data</returns>
    Task<PropertyComparisonDto> ComparePropertiesAsync(int newPropertyId);
}
