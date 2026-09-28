namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Possible outcomes when updating the Apartment QC top section.
/// </summary>
public enum TopSectionUpdateOutcome
{
    Success,
    PropertyNotFound,
    PropertyLocked,
    NoFieldsProvided,
    WingNotFound
}
