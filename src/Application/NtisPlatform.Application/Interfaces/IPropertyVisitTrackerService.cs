using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.PropertyVisitTracker;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyVisitTrackerService
{
    Task<CreatePropertyVisitTrackerResponseDto> CreateVisitAsync(
        CreatePropertyVisitTrackerDto request,
        int loggedInUserId,
        CancellationToken cancellationToken = default);

    Task<PropertyVisitTrackerResponseDto> GetVisitsAsync(
        PropertyVisitTrackerQueryParameters queryParameters,
        int loggedInUserId,
        string? loggedInRole,
        CancellationToken cancellationToken = default);

    Task<CreatePropertySurveyVisitResponseDto> CreateSurveyVisitAsync(
        CreatePropertySurveyVisitDto request,
        int loggedInUserId,
        CancellationToken cancellationToken = default);

    Task<VerifyPropertySurveyVisitResponseDto> VerifyPropertySurveyVisitAsync(
        VerifyPropertySurveyVisitDto request,
        int loggedInUserId,
        CancellationToken cancellationToken = default);

    Task<bool> UnverifyPropertySurveyVisitAsync(
        UnverifyPropertySurveyVisitDto request,
        int loggedInUserId,
        CancellationToken cancellationToken = default);
}
