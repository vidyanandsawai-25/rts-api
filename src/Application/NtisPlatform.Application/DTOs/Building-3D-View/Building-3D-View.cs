namespace NtisPlatform.Application.DTOs.Building3DView;

public class Building3DViewDto
{
    public int MainPropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PropertyName { get; set; }
    public List<Property3DViewDto> MainPropertyDetails { get; set; } = new();
    public List<Amenity3DViewDto> MainPropertyAmenities { get; set; } = new();
    public List<Society3DViewDto> Societies { get; set; } = new();
}

public class Society3DViewDto
{
    public int? SocietyId { get; set; }
    public string? SocietyName { get; set; }
    public int? WingId { get; set; }
    public string? WingName { get; set; }
    public List<Property3DViewDto> Properties { get; set; } = new();
    public List<Amenity3DViewDto> Amenities { get; set; } = new();
}

public class Property3DViewDto
{
    public int PropertyId { get; set; }
    public string? PartitionNo { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? OwnerName { get; set; }
    public string? BHK { get; set; }
    public string? UnitGenerationType { get; set; }
    public int? FloorId { get; set; }
    public string? FloorCode { get; set; }
    public string? FloorDescription { get; set; }
    public bool? IsAssessed { get; set; } = false;
    public bool? IsInternalSarveyVerify { get; set; } = false;
    public bool? IsSubmissionPending { get; set; } = false;
}

public class Amenity3DViewDto
{
    public int PropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? AmenityName { get; set; }
}
