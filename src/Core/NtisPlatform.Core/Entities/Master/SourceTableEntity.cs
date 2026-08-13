using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Configurable source table registered against a module, used to drive dynamic form/grid metadata.
/// </summary>
public class SourceTableEntity : BaseEntity
{
    public int ModuleId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? TableAliasName { get; set; }
    public ModuleMasterEntity? Module { get; set; }
}
