namespace NtisPlatform.Core.Entities;

/// <summary>
/// Urban Local Body Master entity for managing ULB information
/// Maps to Core.ULBMaster table
/// </summary>
public class ULBMasterEntity : BaseEntity
{
    /// <summary>
    /// Primary key - ULB ID
    /// </summary>    /// <summary>
    /// Unique ULB code
    /// </summary>
    public string UlbCode { get; set; } = string.Empty;

    /// <summary>
    /// ULB name in English
    /// </summary>
    public string UlbName { get; set; } = string.Empty;

    /// <summary>
    /// ULB name in local language
    /// </summary>
    public string? UlbNameLocal { get; set; }

    /// <summary>
    /// Type of ULB (Municipality, Corporation, etc.)
    /// </summary>
    public int UlbTypeId { get; set; }

    /// <summary>
    /// ULB Logo path/URL
    /// </summary>
    public string? UlbLogo { get; set; }

    /// <summary>
    /// Official email address
    /// </summary>
    public string? EmailId { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? MobileNo { get; set; }

    /// <summary>
    /// Alternate phone number
    /// </summary>
    public string? AlternateMobileNo { get; set; }

    /// <summary>
    /// Website URL
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Contact person name
    /// </summary>
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Contact person designation
    /// </summary>
    public string? ContactPersonDesignation { get; set; }

    /// <summary>
    /// ULB address
    /// </summary>
    public string? UlbAddress { get; set; }

    /// <summary>
    /// State name
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// PIN code
    /// </summary>
    public string? PinCode { get; set; }

    /// <summary>
    /// Project start date
    /// </summary>
    public DateTime? ProjectStartDate { get; set; }

    /// <summary>
    /// Financial year start date
    /// </summary>
    public DateTime? FinancialYearStartDate { get; set; }

    /// <summary>
    /// Expected go-live date
    /// </summary>
    public DateTime? ExpectedGoLiveDate { get; set; }

    /// <summary>
    /// Partner name
    /// </summary>
    public string? PartnerName { get; set; }

    /// <summary>
    /// Project Manager name
    /// </summary>
    public string? PMName { get; set; }

    /// <summary>
    /// Project Manager email
    /// </summary>
    public string? PMEmailId { get; set; }

    /// <summary>
    /// Project Manager contact number
    /// </summary>
    public string? PMMobileNo { get; set; }

    /// <summary>
    /// License type
    /// </summary>
    public string? LicenceType { get; set; }

    /// <summary>
    /// License start date
    /// </summary>
    public DateTime? LicenceStartDate { get; set; }

    /// <summary>
    /// License end date
    /// </summary>
    public DateTime? LicenceEndDate { get; set; }

    /// <summary>
    /// License duration
    /// </summary>
    public string? LicenceDuration { get; set; }

    /// <summary>
    /// Support type
    /// </summary>
    public string? SupportType { get; set; }

    /// <summary>
    /// License key
    /// </summary>
    public string? LicenceKey { get; set; }    
}
