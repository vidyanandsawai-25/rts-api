namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateMasterDto
{
    public int Id { get; set; }
    public string UpdateCode { get; set; } = string.Empty;
    public string UpdateName { get; set; } = string.Empty;
    public string UpdateNameMarathi { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string ReferenceTableName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DisplaySequence { get; set; }
    public string ApiRoute { get; set; } = string.Empty;
    public string? Description { get; set; }
}
