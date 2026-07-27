namespace NtisPlatform.Core.Exceptions;

/// <summary>
/// Exception thrown when a property is not found
/// </summary>
public class PropertyNotFoundException : EntityNotFoundException
{
    public PropertyNotFoundException(int propertyId)
        : base("Property", propertyId, "PROPERTY_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a property certificate is not found
/// </summary>
public class PropertyCertificateNotFoundException : EntityNotFoundException
{
    public PropertyCertificateNotFoundException(int propertyCertificateId)
        : base("PropertyCertificate", propertyCertificateId, "PROPERTY_CERTIFICATE_NOT_FOUND")
    {
    }

    /// <summary>Used when looking up by (PropertyId, CertificateTypeId, PropertyDetailsId) rather than the internal Id.</summary>
    public PropertyCertificateNotFoundException(string lookupDescription)
        : base("PropertyCertificate", lookupDescription, "PROPERTY_CERTIFICATE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a certificate type is not found
/// </summary>
public class CertificateTypeNotFoundException : EntityNotFoundException
{
    public CertificateTypeNotFoundException(int certificateTypeId)
        : base("CertificateType", certificateTypeId, "CERTIFICATE_TYPE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a property photo is not found
/// </summary>
public class PropertyPhotoNotFoundException : EntityNotFoundException
{
    public PropertyPhotoNotFoundException(int propertyPhotoId)
        : base("PropertyPhoto", propertyPhotoId, "PROPERTY_PHOTO_NOT_FOUND")
    {
    }
}
