namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// What kind of property a search suggestion resolves to, so the frontend knows which screen to
/// navigate to when the user picks it: Society lands on the apartment/society-level screen
/// (which also lists the building's wings, resolved from WingDetailsMast); Unit and
/// IndividualProperty both land on the individual-property screen for that PropertyId.
/// </summary>
public enum ApartmentQcSearchCategory
{
    /// <summary>
    /// Apartment-category property with no PartitionNo -- "apartment society property".
    /// This is the building's representative/common property (may or may not yet have a
    /// SocietyDetailsMast row of its own).
    /// </summary>
    Society = 0,

    /// <summary>
    /// Apartment-category property with a PartitionNo, not an Amenity -- "apartment society unit
    /// property" (an actual flat/shop).
    /// </summary>
    Unit = 1,

    /// <summary>
    /// Not an Apartment-category property -- "individual property".
    /// </summary>
    IndividualProperty = 2,

    /// <summary>
    /// Apartment-category property with a PartitionNo whose PropertyType is classified Amenity
    /// (PropertyTypeMaster.PartType = "Amenity", regardless of whether its description is in
    /// English or Marathi) -- "apartment society amenity property".
    /// </summary>
    Amenity = 3
}

/// <summary>
/// A single wing under a society, returned alongside a Society-category search suggestion.
/// WingDetailsMast has no PropertyId of its own -- wings only exist as children of a society --
/// so they can't be searched/selected directly by WardId+PropertyNo the way a unit or individual
/// property can; instead they're surfaced here once the society is resolved.
/// </summary>
public class ApartmentQcSearchWingSummaryDto
{
    public int SocietyDetailId { get; set; }
    public int WingDetailId { get; set; }
    public string? WingName { get; set; }
}

/// <summary>
/// Query parameters for the ApartmentQC Search typeahead: pick a ward, then type a partial
/// PropertyNo (and optionally PartitionNo) to narrow the suggestion list, mirroring
/// PropertySuggestionQueryParameters / GET api/Property/propwisesearch/suggestions.
/// </summary>
public class ApartmentQcSearchQueryParameters
{
    /// <summary>Ward to scope suggestions to. Required.</summary>
    public int WardId { get; set; }

    /// <summary>Partial PropertyNo term. Matches anywhere in the value.</summary>
    public string? PropertyNo { get; set; }

    /// <summary>Partial PartitionNo term. Matches anywhere in the value.</summary>
    public string? PartitionNo { get; set; }

    /// <summary>Maximum number of suggestions to return. Defaults to 20, capped server-side at 100.</summary>
    public int MaxResults { get; set; } = 20;
}

/// <summary>
/// A single ApartmentQC search suggestion. Extends the plain PropertySuggestionDto shape with a
/// resolved <see cref="Category"/> (Society / Unit / IndividualProperty) and category-specific
/// details, so the frontend can route the user to the correct screen after selection without a
/// second lookup call.
/// </summary>
public class ApartmentQcSearchSuggestionDto
{
    public int PropertyId { get; set; }
    public int ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? UpicId { get; set; }

    /// <summary>Human-readable label for the dropdown, e.g. "123" or "123-A9".</summary>
    public string DisplayLabel { get; set; } = string.Empty;

    public ApartmentQcSearchCategory Category { get; set; }

    /// <summary>
    /// Human-readable label for <see cref="Category"/>: "apartment society property" (Society),
    /// "apartment society unit property" (Unit), "apartment society amenity property" (Amenity),
    /// or "individual property" (IndividualProperty).
    /// </summary>
    public string CategoryLabel { get; set; } = string.Empty;

    /// <summary>Set when Category is Society.</summary>
    public int? SocietyDetailId { get; set; }

    /// <summary>Set when Category is Society.</summary>
    public string? SocietyName { get; set; }

    /// <summary>Set when Category is Society -- every wing under this society (may be empty).</summary>
    public List<ApartmentQcSearchWingSummaryDto>? Wings { get; set; }

    /// <summary>Set when Category is Unit -- the wing this flat belongs to.</summary>
    public int? WingDetailId { get; set; }
}
