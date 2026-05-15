using NtisPlatform.Core.Constants;
using Xunit;

namespace NtisPlatform.Tests.Core.Constants;

/// <summary>
/// Comprehensive tests for DocumentEnumExtensions to achieve 100% line and branch coverage
/// </summary>
public class DocumentEnumExtensionsTests
{
    #region ToModuleString Tests

    [Theory]
    [InlineData(ModuleCode.Property, "PROPERTY")]
    [InlineData(ModuleCode.WaterTax, "WATER_TAX")]
    [InlineData(ModuleCode.Building, "BUILDING")]
    [InlineData(ModuleCode.Asset, "ASSET")]
    [InlineData(ModuleCode.License, "LICENSE")]
    public void ToModuleString_WithValidModuleCode_ReturnsCorrectString(ModuleCode moduleCode, string expected)
    {
        // Act
        var result = moduleCode.ToModuleString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToModuleString_WithInvalidModuleCode_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidModule = (ModuleCode)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => invalidModule.ToModuleString());
        Assert.Contains("Unknown module code", exception.Message);
    }

    #endregion

    #region ParseModuleCode Tests

    [Theory]
    [InlineData("PROPERTY", ModuleCode.Property)]
    [InlineData("property", ModuleCode.Property)]
    [InlineData("Property", ModuleCode.Property)]
    [InlineData("WATER_TAX", ModuleCode.WaterTax)]
    [InlineData("water_tax", ModuleCode.WaterTax)]
    [InlineData("BUILDING", ModuleCode.Building)]
    [InlineData("building", ModuleCode.Building)]
    [InlineData("ASSET", ModuleCode.Asset)]
    [InlineData("asset", ModuleCode.Asset)]
    [InlineData("LICENSE", ModuleCode.License)]
    [InlineData("license", ModuleCode.License)]
    public void ParseModuleCode_WithValidString_ReturnsCorrectEnum(string moduleString, ModuleCode expected)
    {
        // Act
        var result = DocumentEnumExtensions.ParseModuleCode(moduleString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseModuleCode_WithInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseModuleCode("INVALID"));
        Assert.Contains("Unknown module code", exception.Message);
    }

    [Fact]
    public void ParseModuleCode_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseModuleCode(null!));
        Assert.Contains("Unknown module code", exception.Message);
    }

    #endregion

    #region ToStatusString - DocumentUploadStatus Tests

    [Theory]
    [InlineData(DocumentUploadStatus.Active, "ACTIVE")]
    [InlineData(DocumentUploadStatus.Pending, "PENDING")]
    [InlineData(DocumentUploadStatus.Failed, "FAILED")]
    public void ToStatusString_DocumentUploadStatus_ReturnsCorrectString(DocumentUploadStatus status, string expected)
    {
        // Act
        var result = status.ToStatusString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToStatusString_DocumentUploadStatus_WithInvalidValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidStatus = (DocumentUploadStatus)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => invalidStatus.ToStatusString());
        Assert.Contains("Unknown upload status", exception.Message);
    }

    #endregion

    #region ParseUploadStatus Tests

    [Theory]
    [InlineData("ACTIVE", DocumentUploadStatus.Active)]
    [InlineData("active", DocumentUploadStatus.Active)]
    [InlineData("Active", DocumentUploadStatus.Active)]
    [InlineData("PENDING", DocumentUploadStatus.Pending)]
    [InlineData("pending", DocumentUploadStatus.Pending)]
    [InlineData("FAILED", DocumentUploadStatus.Failed)]
    [InlineData("failed", DocumentUploadStatus.Failed)]
    public void ParseUploadStatus_WithValidString_ReturnsCorrectEnum(string statusString, DocumentUploadStatus expected)
    {
        // Act
        var result = DocumentEnumExtensions.ParseUploadStatus(statusString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseUploadStatus_WithInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseUploadStatus("INVALID"));
        Assert.Contains("Unknown upload status", exception.Message);
    }

    [Fact]
    public void ParseUploadStatus_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseUploadStatus(null!));
        Assert.Contains("Unknown upload status", exception.Message);
    }

    #endregion

    #region ToStatusString - DocumentScanStatus Tests

    [Theory]
    [InlineData(DocumentScanStatus.Pending, "PENDING")]
    [InlineData(DocumentScanStatus.Clean, "CLEAN")]
    [InlineData(DocumentScanStatus.Infected, "INFECTED")]
    [InlineData(DocumentScanStatus.Error, "ERROR")]
    public void ToStatusString_DocumentScanStatus_ReturnsCorrectString(DocumentScanStatus status, string expected)
    {
        // Act
        var result = status.ToStatusString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToStatusString_DocumentScanStatus_WithInvalidValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidStatus = (DocumentScanStatus)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => invalidStatus.ToStatusString());
        Assert.Contains("Unknown scan status", exception.Message);
    }

    #endregion

    #region ParseScanStatus Tests

    [Theory]
    [InlineData("PENDING", DocumentScanStatus.Pending)]
    [InlineData("pending", DocumentScanStatus.Pending)]
    [InlineData("Pending", DocumentScanStatus.Pending)]
    [InlineData("CLEAN", DocumentScanStatus.Clean)]
    [InlineData("clean", DocumentScanStatus.Clean)]
    [InlineData("INFECTED", DocumentScanStatus.Infected)]
    [InlineData("infected", DocumentScanStatus.Infected)]
    [InlineData("ERROR", DocumentScanStatus.Error)]
    [InlineData("error", DocumentScanStatus.Error)]
    public void ParseScanStatus_WithValidString_ReturnsCorrectEnum(string statusString, DocumentScanStatus expected)
    {
        // Act
        var result = DocumentEnumExtensions.ParseScanStatus(statusString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseScanStatus_WithInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseScanStatus("INVALID"));
        Assert.Contains("Unknown scan status", exception.Message);
    }

    [Fact]
    public void ParseScanStatus_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseScanStatus(null!));
        Assert.Contains("Unknown scan status", exception.Message);
    }

    #endregion

    #region ToTypeString Tests

    [Theory]
    [InlineData(DocumentType.Certificate, "Certificate")]
    [InlineData(DocumentType.Permit, "Permit")]
    [InlineData(DocumentType.Invoice, "Invoice")]
    [InlineData(DocumentType.Contract, "Contract")]
    [InlineData(DocumentType.Report, "Report")]
    [InlineData(DocumentType.Proof, "Proof")]
    [InlineData(DocumentType.Application, "Application")]
    [InlineData(DocumentType.Approval, "Approval")]
    public void ToTypeString_WithValidDocumentType_ReturnsCorrectString(DocumentType type, string expected)
    {
        // Act
        var result = type.ToTypeString();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ParseDocumentType Tests

    [Theory]
    [InlineData("Certificate", DocumentType.Certificate)]
    [InlineData("certificate", DocumentType.Certificate)]
    [InlineData("CERTIFICATE", DocumentType.Certificate)]
    [InlineData("Permit", DocumentType.Permit)]
    [InlineData("permit", DocumentType.Permit)]
    [InlineData("Invoice", DocumentType.Invoice)]
    [InlineData("Contract", DocumentType.Contract)]
    [InlineData("Report", DocumentType.Report)]
    [InlineData("Proof", DocumentType.Proof)]
    [InlineData("Application", DocumentType.Application)]
    [InlineData("Approval", DocumentType.Approval)]
    public void ParseDocumentType_WithValidString_ReturnsCorrectEnum(string typeString, DocumentType expected)
    {
        // Act
        var result = DocumentEnumExtensions.ParseDocumentType(typeString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseDocumentType_WithInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseDocumentType("INVALID"));
        Assert.Contains("Unknown document type", exception.Message);
    }

    #endregion

    #region ToPurposeString Tests

    [Theory]
    [InlineData(DocumentBindingPurpose.MainDocument, "MainDocument")]
    [InlineData(DocumentBindingPurpose.SupportingDocument, "SupportingDocument")]
    [InlineData(DocumentBindingPurpose.ProofDocument, "ProofDocument")]
    [InlineData(DocumentBindingPurpose.ApprovalDocument, "ApprovalDocument")]
    [InlineData(DocumentBindingPurpose.ApplicationDocument, "ApplicationDocument")]
    public void ToPurposeString_WithValidBindingPurpose_ReturnsCorrectString(DocumentBindingPurpose purpose, string expected)
    {
        // Act
        var result = purpose.ToPurposeString();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ParseBindingPurpose Tests

    [Theory]
    [InlineData("MainDocument", DocumentBindingPurpose.MainDocument)]
    [InlineData("maindocument", DocumentBindingPurpose.MainDocument)]
    [InlineData("MAINDOCUMENT", DocumentBindingPurpose.MainDocument)]
    [InlineData("SupportingDocument", DocumentBindingPurpose.SupportingDocument)]
    [InlineData("supportingdocument", DocumentBindingPurpose.SupportingDocument)]
    [InlineData("ProofDocument", DocumentBindingPurpose.ProofDocument)]
    [InlineData("ApprovalDocument", DocumentBindingPurpose.ApprovalDocument)]
    [InlineData("ApplicationDocument", DocumentBindingPurpose.ApplicationDocument)]
    public void ParseBindingPurpose_WithValidString_ReturnsCorrectEnum(string purposeString, DocumentBindingPurpose expected)
    {
        // Act
        var result = DocumentEnumExtensions.ParseBindingPurpose(purposeString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseBindingPurpose_WithInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseBindingPurpose("INVALID"));
        Assert.Contains("Unknown binding purpose", exception.Message);
    }

    #endregion

    #region ToTableString Tests

    [Theory]
    [InlineData(DocumentReferenceTable.PropertyCertificate, "PropertyCertificate")]
    [InlineData(DocumentReferenceTable.PropertyDiscount, "PropertyDiscount")]
    [InlineData(DocumentReferenceTable.PropertyOwner, "PropertyOwner")]
    [InlineData(DocumentReferenceTable.BuildingPermission, "BuildingPermission")]
    [InlineData(DocumentReferenceTable.BuildingPlan, "BuildingPlan")]
    [InlineData(DocumentReferenceTable.WaterConnection, "WaterConnection")]
    [InlineData(DocumentReferenceTable.WaterBill, "WaterBill")]
    [InlineData(DocumentReferenceTable.AssetDocument, "AssetDocument")]
    [InlineData(DocumentReferenceTable.TradeLicense, "TradeLicense")]
    public void ToTableString_WithValidReferenceTable_ReturnsCorrectString(DocumentReferenceTable table, string expected)
    {
        // Act
        var result = table.ToTableString();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ParseReferenceTable Tests

    [Theory]
    [InlineData("PropertyCertificate", DocumentReferenceTable.PropertyCertificate)]
    [InlineData("propertycertificate", DocumentReferenceTable.PropertyCertificate)]
    [InlineData("PROPERTYCERTIFICATE", DocumentReferenceTable.PropertyCertificate)]
    [InlineData("PropertyDiscount", DocumentReferenceTable.PropertyDiscount)]
    [InlineData("PropertyOwner", DocumentReferenceTable.PropertyOwner)]
    [InlineData("BuildingPermission", DocumentReferenceTable.BuildingPermission)]
    [InlineData("BuildingPlan", DocumentReferenceTable.BuildingPlan)]
    [InlineData("WaterConnection", DocumentReferenceTable.WaterConnection)]
    [InlineData("WaterBill", DocumentReferenceTable.WaterBill)]
    [InlineData("AssetDocument", DocumentReferenceTable.AssetDocument)]
    [InlineData("TradeLicense", DocumentReferenceTable.TradeLicense)]
    public void ParseReferenceTable_WithValidString_ReturnsCorrectEnum(string tableString, DocumentReferenceTable expected)
    {
        // Act
        var result = DocumentEnumExtensions.ParseReferenceTable(tableString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseReferenceTable_WithInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            DocumentEnumExtensions.ParseReferenceTable("INVALID"));
        Assert.Contains("Unknown reference table", exception.Message);
    }

    #endregion

    #region Round-trip Conversion Tests

    [Fact]
    public void ModuleCode_RoundTripConversion_RetainsValue()
    {
        // Arrange
        var original = ModuleCode.Property;

        // Act
        var stringValue = original.ToModuleString();
        var parsed = DocumentEnumExtensions.ParseModuleCode(stringValue);

        // Assert
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void DocumentUploadStatus_RoundTripConversion_RetainsValue()
    {
        // Arrange
        var original = DocumentUploadStatus.Active;

        // Act
        var stringValue = original.ToStatusString();
        var parsed = DocumentEnumExtensions.ParseUploadStatus(stringValue);

        // Assert
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void DocumentScanStatus_RoundTripConversion_RetainsValue()
    {
        // Arrange
        var original = DocumentScanStatus.Clean;

        // Act
        var stringValue = original.ToStatusString();
        var parsed = DocumentEnumExtensions.ParseScanStatus(stringValue);

        // Assert
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void DocumentType_RoundTripConversion_RetainsValue()
    {
        // Arrange
        var original = DocumentType.Certificate;

        // Act
        var stringValue = original.ToTypeString();
        var parsed = DocumentEnumExtensions.ParseDocumentType(stringValue);

        // Assert
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void DocumentBindingPurpose_RoundTripConversion_RetainsValue()
    {
        // Arrange
        var original = DocumentBindingPurpose.MainDocument;

        // Act
        var stringValue = original.ToPurposeString();
        var parsed = DocumentEnumExtensions.ParseBindingPurpose(stringValue);

        // Assert
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void DocumentReferenceTable_RoundTripConversion_RetainsValue()
    {
        // Arrange
        var original = DocumentReferenceTable.PropertyCertificate;

        // Act
        var stringValue = original.ToTableString();
        var parsed = DocumentEnumExtensions.ParseReferenceTable(stringValue);

        // Assert
        Assert.Equal(original, parsed);
    }

    #endregion
}
