namespace NtisPlatform.Core.Entities.Master;

public class RTSServiceOfficerAllocationEntity : BaseEntity
{
    public int ServiceId { get; set; }
    public int? ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string? ZoneNameLocal { get; set; }
    public string OfficerName { get; set; } = string.Empty;
    public string? OfficerNameLocal { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string? DesignationLocal { get; set; }
    public string MobileNo { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? OfficeAddress { get; set; }
    public string? OfficeAddressLocal { get; set; }
    public string OfficerRole { get; set; } = "DesignatedOfficer";
    public int DisplayOrder { get; set; } = 1;

    public virtual RTSServiceEntity? Service { get; set; }
}
