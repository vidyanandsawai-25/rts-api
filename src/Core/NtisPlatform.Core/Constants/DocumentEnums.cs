namespace NtisPlatform.Core.Constants;

/// <summary>
/// Type-safe document type enumeration
/// Replaces DocumentConstants.DocumentType nested class
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Certificate documents (e.g., Property Certificates, Birth Certificates)
    /// </summary>
    Certificate,

    /// <summary>
    /// Permit documents (e.g., Building Permits, Business Permits)
    /// </summary>
    Permit,

    /// <summary>
    /// Invoice and billing documents
    /// </summary>
    Invoice,

    /// <summary>
    /// Contract and agreement documents
    /// </summary>
    Contract,

    /// <summary>
    /// Report documents (e.g., Inspection Reports, Audit Reports)
    /// </summary>
    Report,

    /// <summary>
    /// Proof documents (e.g., Identity Proof, Address Proof)
    /// </summary>
    Proof,

    /// <summary>
    /// Application documents
    /// </summary>
    Application,

    /// <summary>
    /// Approval documents
    /// </summary>
    Approval
}

/// <summary>
/// Type-safe module code enumeration
/// Replaces DocumentConstants.Module nested class
/// </summary>
public enum ModuleCode
{
    /// <summary>
    /// Property Tax Information System
    /// </summary>
    Property,

    /// <summary>
    /// Water Tax Management
    /// </summary>
    WaterTax,

    /// <summary>
    /// Building Permission and Management
    /// </summary>
    Building,

    /// <summary>
    /// Asset Management
    /// </summary>
    Asset,

    /// <summary>
    /// License Management (Trade, Business, etc.)
    /// </summary>
    License
}

/// <summary>
/// Type-safe upload status enumeration
/// Replaces DocumentConstants.UploadStatus nested class
/// </summary>
public enum DocumentUploadStatus
{
    /// <summary>
    /// Document upload is active and accessible
    /// </summary>
    Active,

    /// <summary>
    /// Document upload is pending completion or processing
    /// </summary>
    Pending,

    /// <summary>
    /// Document upload failed
    /// </summary>
    Failed
}

/// <summary>
/// Type-safe scan status enumeration for virus/malware scanning
/// Replaces DocumentConstants.ScanStatus nested class
/// </summary>
public enum DocumentScanStatus
{
    /// <summary>
    /// Document scan is pending
    /// </summary>
    Pending,

    /// <summary>
    /// Document is clean (no threats detected)
    /// </summary>
    Clean,

    /// <summary>
    /// Document is infected with virus/malware
    /// </summary>
    Infected,

    /// <summary>
    /// Error occurred during scanning
    /// </summary>
    Error
}

/// <summary>
/// Type-safe binding purpose enumeration
/// Replaces DocumentConstants.BindingPurpose nested class
/// </summary>
public enum DocumentBindingPurpose
{
    /// <summary>
    /// Primary/main document for the entity
    /// </summary>
    MainDocument,

    /// <summary>
    /// Supporting documentation
    /// </summary>
    SupportingDocument,

    /// <summary>
    /// Proof or evidence document
    /// </summary>
    ProofDocument,

    /// <summary>
    /// Approval or authorization document
    /// </summary>
    ApprovalDocument,

    /// <summary>
    /// Application form or submission document
    /// </summary>
    ApplicationDocument
}

/// <summary>
/// Type-safe reference table enumeration
/// Replaces DocumentConstants.ReferenceTable nested class
/// Note: This is a standard C# enum (int-backed by default); any string/database mapping
/// should be handled explicitly by the persistence or serialization layer if required.
/// </summary>
public enum DocumentReferenceTable
{
    // PROPERTY module tables
    PropertyCertificate,
    PropertyDiscount,
    PropertyOwner,

    // BUILDING module tables
    BuildingPermission,
    BuildingPlan,

    // WATER_TAX module tables
    WaterConnection,
    WaterBill,

    // ASSET module tables
    AssetDocument,

    // LICENSE module tables
    TradeLicense
}

/// <summary>
/// Extension methods for enum to string conversion
/// Provides backward compatibility with existing string-based constants
/// </summary>
public static class DocumentEnumExtensions
{
    /// <summary>
    /// Converts ModuleCode enum to uppercase string format used in database
    /// </summary>
    public static string ToModuleString(this ModuleCode module)
    {
        return module switch
        {
            ModuleCode.Property => "PROPERTY",
            ModuleCode.WaterTax => "WATER_TAX",
            ModuleCode.Building => "BUILDING",
            ModuleCode.Asset => "ASSET",
            ModuleCode.License => "LICENSE",
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unknown module code")
        };
    }

    /// <summary>
    /// Parses string to ModuleCode enum
    /// </summary>
    public static ModuleCode ParseModuleCode(string moduleString)
    {
        return moduleString?.ToUpperInvariant() switch
        {
            "PROPERTY" => ModuleCode.Property,
            "WATER_TAX" => ModuleCode.WaterTax,
            "BUILDING" => ModuleCode.Building,
            "ASSET" => ModuleCode.Asset,
            "LICENSE" => ModuleCode.License,
            _ => throw new ArgumentException($"Unknown module code: {moduleString}", nameof(moduleString))
        };
    }

    /// <summary>
    /// Converts DocumentUploadStatus enum to uppercase string format
    /// </summary>
    public static string ToStatusString(this DocumentUploadStatus status)
    {
        return status switch
        {
            DocumentUploadStatus.Active => "ACTIVE",
            DocumentUploadStatus.Pending => "PENDING",
            DocumentUploadStatus.Failed => "FAILED",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown upload status")
        };
    }

    /// <summary>
    /// Parses string to DocumentUploadStatus enum
    /// </summary>
    public static DocumentUploadStatus ParseUploadStatus(string statusString)
    {
        return statusString?.ToUpperInvariant() switch
        {
            "ACTIVE" => DocumentUploadStatus.Active,
            "PENDING" => DocumentUploadStatus.Pending,
            "FAILED" => DocumentUploadStatus.Failed,
            _ => throw new ArgumentException($"Unknown upload status: {statusString}", nameof(statusString))
        };
    }

    /// <summary>
    /// Converts DocumentScanStatus enum to uppercase string format
    /// </summary>
    public static string ToStatusString(this DocumentScanStatus status)
    {
        return status switch
        {
            DocumentScanStatus.Pending => "PENDING",
            DocumentScanStatus.Clean => "CLEAN",
            DocumentScanStatus.Infected => "INFECTED",
            DocumentScanStatus.Error => "ERROR",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown scan status")
        };
    }

    /// <summary>
    /// Parses string to DocumentScanStatus enum
    /// </summary>
    public static DocumentScanStatus ParseScanStatus(string statusString)
    {
        return statusString?.ToUpperInvariant() switch
        {
            "PENDING" => DocumentScanStatus.Pending,
            "CLEAN" => DocumentScanStatus.Clean,
            "INFECTED" => DocumentScanStatus.Infected,
            "ERROR" => DocumentScanStatus.Error,
            _ => throw new ArgumentException($"Unknown scan status: {statusString}", nameof(statusString))
        };
    }

    /// <summary>
    /// Converts DocumentType enum to string format
    /// </summary>
    public static string ToTypeString(this DocumentType type)
    {
        return type.ToString();
    }

    /// <summary>
    /// Parses string to DocumentType enum
    /// </summary>
    public static DocumentType ParseDocumentType(string typeString)
    {
        if (Enum.TryParse<DocumentType>(typeString, true, out var result))
        {
            return result;
        }
        throw new ArgumentException($"Unknown document type: {typeString}", nameof(typeString));
    }

    /// <summary>
    /// Converts DocumentBindingPurpose enum to string format
    /// </summary>
    public static string ToPurposeString(this DocumentBindingPurpose purpose)
    {
        return purpose.ToString();
    }

    /// <summary>
    /// Parses string to DocumentBindingPurpose enum
    /// </summary>
    public static DocumentBindingPurpose ParseBindingPurpose(string purposeString)
    {
        if (Enum.TryParse<DocumentBindingPurpose>(purposeString, true, out var result))
        {
            return result;
        }
        throw new ArgumentException($"Unknown binding purpose: {purposeString}", nameof(purposeString));
    }

    /// <summary>
    /// Converts DocumentReferenceTable enum to string format
    /// </summary>
    public static string ToTableString(this DocumentReferenceTable table)
    {
        return table.ToString();
    }

    /// <summary>
    /// Parses string to DocumentReferenceTable enum
    /// </summary>
    public static DocumentReferenceTable ParseReferenceTable(string tableString)
    {
        if (Enum.TryParse<DocumentReferenceTable>(tableString, true, out var result))
        {
            return result;
        }
        throw new ArgumentException($"Unknown reference table: {tableString}", nameof(tableString));
    }
}
