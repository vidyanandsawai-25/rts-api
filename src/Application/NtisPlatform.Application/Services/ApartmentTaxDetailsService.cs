using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application-layer service for the apartment tax-details panel. Assembles raw data fetched via
/// <see cref="IApartmentTaxDetailsRepository"/> into the API-facing DTO shape.
/// </summary>
public class ApartmentTaxDetailsService : IApartmentTaxDetailsService
{
    private readonly IApartmentTaxDetailsRepository _repository;

    public ApartmentTaxDetailsService(IApartmentTaxDetailsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApartmentTaxDetailsDto?> GetTaxDetailsAsync(ApartmentTaxDetailsQueryParameters query, CancellationToken cancellationToken = default)
    {
        var raw = await _repository.GetTaxDetailsAsync(query, cancellationToken);
        if (raw is null)
        {
            return null;
        }

        return new ApartmentTaxDetailsDto
        {
            WardId = query.WardId,
            PropertyNo = query.PropertyNo,
            WingMasterId = raw.WingMasterId,
            WingName = raw.WingName,
            WingNo = raw.WingNo,
            SocietyName = raw.SocietyName,
            PropertyCount = raw.PropertyCount,
            CurrentTaxes = raw.CurrentTaxes,
            Arrears = raw.Arrears,
        };
    }
}
