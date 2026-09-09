using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application-layer service for the ApartmentQC certificate grid. Uses the same identifier
/// precedence as <see cref="IApartmentQcTopSectionRepository"/>'s top-section panel, but scopes
/// the resulting grid to what the supplied identifier actually identifies (one unit, one wing, or
/// the whole apartment) rather than always expanding out to the whole apartment.
/// </summary>
public class ApartmentQcCertificateGridService : IApartmentQcCertificateGridService
{
    private readonly IApartmentQcTopSectionRepository _topSectionRepository;
    private readonly IApartmentQcCertificateGridRepository _repository;

    public ApartmentQcCertificateGridService(
        IApartmentQcTopSectionRepository topSectionRepository,
        IApartmentQcCertificateGridRepository repository)
    {
        _topSectionRepository = topSectionRepository;
        _repository = repository;
    }

    public async Task<ApartmentQcCertificateGridDto?> GetGridAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default)
    {
        // Same "first satisfied wins" precedence as GET api/ApartmentQcTopSection, but each
        // identifier now scopes the grid to what it actually identifies, instead of always
        // expanding out to the whole apartment:
        //   PropertyId / Upic / WardId+PropertyNo+PartitionNo -> pins one unit -> unit-scoped grid.
        //   WardId+PropertyNo alone (no partition) -> ambiguous across units -> apartment-wide grid.
        //   WingDetailsId -> wing-scoped grid.
        //   SocietyId -> apartment-wide grid.
        var isSingleUnit = query.PropertyId.HasValue
            || !string.IsNullOrWhiteSpace(query.Upic)
            || (query.WardId.HasValue && !string.IsNullOrWhiteSpace(query.PropertyNo) && !string.IsNullOrWhiteSpace(query.PartitionNo));

        if (isSingleUnit)
        {
            var unitProperty = await _topSectionRepository.GetPropertyAsync(query, cancellationToken);
            if (unitProperty is null)
            {
                return null;
            }

            return await _repository.GetGridForUnitAsync(unitProperty.Id, cancellationToken);
        }

        var isWingOnly = !(query.WardId.HasValue && !string.IsNullOrWhiteSpace(query.PropertyNo)) && query.WingDetailsId.HasValue;
        if (isWingOnly)
        {
            return await _repository.GetGridForWingAsync(query.WingDetailsId!.Value, cancellationToken);
        }

        var property = await _topSectionRepository.GetPropertyAsync(query, cancellationToken);
        if (property is null || string.IsNullOrWhiteSpace(property.PropertyNo))
        {
            return null;
        }

        return await _repository.GetGridAsync(property.WardId, property.PropertyNo, cancellationToken);
    }
}
