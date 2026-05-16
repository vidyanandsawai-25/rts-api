namespace NtisPlatform.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class IsLocalizableAttribute : Attribute
{
    public string Resource { get; }

    public IsLocalizableAttribute(string resource)
    {
        Resource = resource;
    }
}
