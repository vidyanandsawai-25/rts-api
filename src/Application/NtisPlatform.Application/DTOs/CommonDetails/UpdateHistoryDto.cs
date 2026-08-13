using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class UpdateHistoryQueryParameters : BaseQueryParameters
{
    public string? UpdateName { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? UpdatedColumns { get; set; }
    public string? Username { get; set; }
}

public class UpdateHistoryDto
{
    public int Id { get; set; }
    public string? UpdateName { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string Property { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UpdatedColumns { get; set; }
    public string? Remarks { get; set; }
    public string? IPAddress { get; set; }
    public string? Username { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
