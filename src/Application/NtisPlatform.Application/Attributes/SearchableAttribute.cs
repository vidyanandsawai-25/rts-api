namespace NtisPlatform.Application.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SearchableAttribute : Attribute
{
    public string? EntityProperty { get; set; }

    public SearchableAttribute()
    {
    }

    public SearchableAttribute(string entityProperty)
    {
        EntityProperty = entityProperty;
    }
}
