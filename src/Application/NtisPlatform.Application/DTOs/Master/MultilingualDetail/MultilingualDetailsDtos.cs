namespace NtisPlatform.Application.DTOs;

public class MultilingualDetailsDtos
{
    public int Id { get; set; }
    public string? Resource { get; set; } = string.Empty;
    public string? Key { get; set; } = string.Empty;
    public string? Culture { get; set; } = string.Empty;
    public string? Value { get; set; } = string.Empty;

    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateMultilingualDetailsDtos
{
    public int Id { get; set; }
    public string? Resource { get; set; } = string.Empty;
    public string? Key { get; set; } = string.Empty;
    public string? Culture { get; set; } = string.Empty;
    public string? Value { get; set; } = string.Empty;

    public int? CreatedBy { get; set; }
}

public class UpdateMultilingualDetailsDtos
{
    public int Id { get; set; }
    public string? Resource { get; set; } = string.Empty;
    public string? Key { get; set; } = string.Empty;
    public string? Culture { get; set; } = string.Empty;
    public string? Value { get; set; } = string.Empty;
    public int? UpdatedBy { get; set; }
}