namespace NtisPlatform.Core;

/// <summary>
/// Specifies the Entity type(s) that this DTO represents for localization.
/// Use this when DTO naming doesn't follow convention: {BaseName}Dto → {BaseName}Entity
/// 
/// Supports multiple entities when one DTO is shared across entities.
/// Place on DTO class (Application layer can reference Core layer - Clean Architecture compliant).
/// </summary>
/// <example>
/// // Single entity (name doesn't match convention)
/// [LocalizableEntity(typeof(PropertyMasterEntity))]
/// public class PropertyDto { ... }
/// 
/// // Multiple entities share same DTO
/// [LocalizableEntity(typeof(CustomerAddressEntity), typeof(VendorAddressEntity))]
/// public class AddressDto { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LocalizableEntityAttribute : Attribute
{
    /// <summary>
    /// The Entity types that this DTO represents.
    /// </summary>
    public Type[] EntityTypes { get; }

    public LocalizableEntityAttribute(params Type[] entityTypes)
    {
        if (entityTypes == null || entityTypes.Length == 0)
            throw new ArgumentException("At least one entity type must be specified.", nameof(entityTypes));

        EntityTypes = entityTypes;
    }
}