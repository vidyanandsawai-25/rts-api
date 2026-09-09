namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

public class WingWiseDetailsQueryParameters
{
    public int? WardId { get; set; }
    public string? PropertyNo { get; set; }
}

public class WingWiseDetailsResponseDto
{
    public int PropertyId { get; set; }
    public string PropertyNo { get; set; } = string.Empty;
    public string WardNo { get; set; } = string.Empty;
    public int? SocietyId { get; set; }
    public IReadOnlyList<WingWiseDetailDto> Wings { get; set; } = Array.Empty<WingWiseDetailDto>();
}

public class WingWiseDetailDto
{
    public int? WingId { get; set; }
    public int? WingMasterId { get; set; }
    public int? WingDetailId { get; set; }
    public int? SocietyId { get; set; }
    public string WingNo { get; set; } = string.Empty;
    public string? WingName { get; set; }
    public int PropertyCount { get; set; }
    public string FloorRange { get; set; } = string.Empty;
    public decimal TotalArea { get; set; }
    public decimal CollectionPercentage { get; set; }
    public string OldDemand { get; set; } = "0";
    public string CurrentDemand { get; set; } = "0";
    public string RetroDemand { get; set; } = "0";
    public string TotalDemand { get; set; } = "0";
    public string RevenueImpact { get; set; } = "0";
    public decimal RevenueImpactPercentage { get; set; }
    public IReadOnlyList<RevenueImpactDetailDto> RevenueImpactDetails { get; set; } = Array.Empty<RevenueImpactDetailDto>();
    public string ExemptionAppliedAmount { get; set; } = "0";
    public int ExemptedPropertyCount { get; set; }
    public IReadOnlyList<WingWisePropertyTypeDetailDto> PropertyTypes { get; set; } = Array.Empty<WingWisePropertyTypeDetailDto>();
}

public class RevenueImpactDetailDto
{
    public string PreviousRV { get; set; } = "0";
    public string RevisedRV { get; set; } = "0";
    public string DifferenceRV { get; set; } = "0";
    public int AffectedUnits { get; set; }
}

public class WingWisePropertyTypeDetailDto
{
    public string PropertyTypeName { get; set; } = string.Empty;
    public int PropertyCount { get; set; }
    public decimal TotalArea { get; set; }
    public string OldDemand { get; set; } = "0";
    public string CurrentDemand { get; set; } = "0";
    public string RetroDemand { get; set; } = "0";
    public string TotalDemand { get; set; } = "0";
    public string TotalRevenue { get; set; } = "0";
}



