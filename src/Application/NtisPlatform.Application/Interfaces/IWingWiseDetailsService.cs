using NtisPlatform.Application.DTOs.Property.ApartmentQC;

namespace NtisPlatform.Application.Interfaces;

public interface IWingWiseDetailsService
{
    Task<WingWiseDetailsResponseDto> GetWingWiseDetailsAsync(
        WingWiseDetailsQueryParameters query,
        CancellationToken cancellationToken = default);
}
