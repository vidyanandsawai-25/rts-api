

namespace NtisPlatform.Application.DTOs;

public class PTISConstructionTypeMasterDto
{

    public string ConstructionId { get; set; }
    public string Description { get; set; }

}

public class PTISConstructionTypeMasterDtoResponse
{
    public string Message { get; set; } = string.Empty;
}
public class PTISFloorMasterDto
{

    public string? FloorID { get; set; }
    public string? Description { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }= DateTime.Now;
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }= DateTime.Now;
}
