namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Outcome of a PATCH <c>apartmentqc/{propertyId}</c> bulk update.
/// Updates are atomic: <see cref="Updated"/> equals <see cref="TotalRequested"/> only when
/// <see cref="Failures"/> is empty. When validation produces any failure, no rows are written.
/// </summary>
public sealed class ApartmentQCBulkUpdateResultDto
{
    public int TotalRequested { get; init; }
    public int Updated { get; init; }
    public IReadOnlyList<int> UpdatedDetailIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<ApartmentQCBulkUpdateFailureDto> Failures { get; init; } = Array.Empty<ApartmentQCBulkUpdateFailureDto>();
}

/// <summary>Describes a single per-row failure in the bulk update result.</summary>
public sealed class ApartmentQCBulkUpdateFailureDto
{
    public int DetailId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Field { get; init; }
    public int? InvalidId { get; init; }
}
