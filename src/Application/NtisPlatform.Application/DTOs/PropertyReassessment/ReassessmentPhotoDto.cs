namespace NtisPlatform.Application.DTOs.PropertyReassessment;

/// <summary>
/// A single photo/plan document for the re-assessment screen (STEP 2 of the SQL).
/// <see cref="Type"/> identifies which slot the document fills.
/// </summary>
public class ReassessmentPhotoDto
{
    /// <summary>Document GUID used by the client to fetch/render the image.</summary>
    public Guid DocumentGuid { get; set; }

    /// <summary>
    /// One of: OLD_PLAN_PHOTO, OLD_PROPERTY_PHOTO, NEW_PLAN_PHOTO, NEW_PROPERTY_PHOTO.
    /// "OLD_*" = Municipal Corp. Registration (IsLatest = false); "NEW_*" = New Survey (IsLatest = true).
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
