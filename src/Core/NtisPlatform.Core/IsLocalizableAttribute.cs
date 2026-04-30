namespace NtisPlatform.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class IsLocalizableAttribute : Attribute
{
    public string Resource { get; }
    
    /// <summary>
    /// Optional: Name of the property on the DTO that holds the entity's ID.
    /// If not set, defaults to "Id".
    /// Used to generate stable localization keys: {Resource}_{EntityId}_{PropertyName}
    /// </summary>
    public string IdProperty { get; set; } = "Id";

    public IsLocalizableAttribute(string resource)
    {
        Resource = resource;
    }
}
