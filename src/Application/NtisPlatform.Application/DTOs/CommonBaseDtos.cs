namespace NtisPlatform.Application.DTOs;

public class CommonBaseDtos
{
    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateCommonBaseDtos
{
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
}
public class UpdateCommonBaseDtos
{
    public bool IsActive { get; set; }
    public int? UpdatedBy { get; set; }
}
