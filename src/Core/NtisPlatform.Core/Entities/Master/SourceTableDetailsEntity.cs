namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Field-level metadata for a configurable <see cref="SourceTableEntity"/>.
/// </summary>
public class SourceTableDetailsEntity : BaseEntity
{
    public int SourceTableId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ControlType { get; set; }
    public string? DataType { get; set; }
    public string? Placeholder { get; set; }
    public int? MaxLength { get; set; }
    public string? ValidationRegex { get; set; }
    public string? DefaultValue { get; set; }
    public string? BindApi { get; set; }
    public string? ApiResponse { get; set; }
    public bool IsRequired { get; set; }
    public int SequenceNo { get; set; }

    public SourceTableEntity? SourceTable { get; set; }
}
