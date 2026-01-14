namespace NtisPlatform.Application.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SortableAttribute : Attribute
{
    public string? EntityProperty { get; set; }

    public SortableAttribute()
    {
    }

    public SortableAttribute(string entityProperty)
    {
        EntityProperty = entityProperty;
    }
}
