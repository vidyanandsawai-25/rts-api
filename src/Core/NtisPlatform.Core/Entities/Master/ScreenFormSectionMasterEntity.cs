namespace NtisPlatform.Core.Entities.Master;

public class ScreenFormSectionMasterEntity : BaseEntity
{
    public int ScreenId { get; set; }

    public int? ParentSectionId { get; set; }

    public string SectionType { get; set; } = null!;

    public string SectionName { get; set; } = null!;
    public string? SectionNameLocal { get; set; }

    public string SectionCode { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public int ColumnCount { get; set; }

    public bool IsOptional { get; set; }

    public bool IsCollapsible { get; set; }

    public bool IsCollapsedByDefault { get; set; }

    public bool IsRepeatable { get; set; }
}