namespace NtisPlatform.Application.DTOs;

public class BaseDtos
{
    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateBaseDtos
{
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
}
public class UpdateBaseDtos
{
    public bool IsActive { get; set; }
    public int? UpdatedBy { get; set; }
}
