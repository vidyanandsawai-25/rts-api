using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertySurveySearch;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertySurveyService
{
    Task<UserPropertyPageDto> SearchNewlyCreatedPropertiesAsync(
        CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken = default);
}
