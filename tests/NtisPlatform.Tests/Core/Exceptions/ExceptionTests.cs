using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

/// <summary>
/// Comprehensive tests for all exception classes to achieve 100% line and branch coverage
/// </summary>
public class ExceptionTests
{
    #region NtisPlatformException Tests

    [Fact]
    public void NtisPlatformException_WithMessageAndErrorCode_SetsProperties()
    {
        // Arrange & Act
        var exception = new TestNtisPlatformException("Test message", "TEST_ERROR");

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal("TEST_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void NtisPlatformException_WithInnerException_SetsProperties()
    {
        // Arrange
        var inner = new Exception("Inner error");

        // Act
        var exception = new TestNtisPlatformExceptionWithInner("Test message", "TEST_ERROR", inner);

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal("TEST_ERROR", exception.ErrorCode);
        Assert.Same(inner, exception.InnerException);
    }

    #endregion

    #region EntityNotFoundException Tests

    [Fact]
    public void EntityNotFoundException_WithIntId_SetsProperties()
    {
        // Arrange & Act
        var exception = new TestEntityNotFoundException("TestEntity", 123);

        // Assert
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains("123", exception.Message);
        Assert.Equal("TestEntity", exception.EntityType);
        Assert.Equal(123, exception.EntityId);
        Assert.Equal("TEST_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void EntityNotFoundException_WithGuidId_SetsProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var exception = new TestEntityNotFoundException("TestEntity", guid);

        // Assert
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains(guid.ToString(), exception.Message);
        Assert.Equal("TestEntity", exception.EntityType);
        Assert.Equal(guid, exception.EntityId);
    }

    #endregion

    #region ValidationException Tests

    [Fact]
    public void ValidationException_WithMessage_SetsProperties()
    {
        // Arrange & Act
        var exception = new TestValidationException("Validation failed", "VALIDATION_ERROR");

        // Assert
        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void ValidationException_WithInnerException_SetsProperties()
    {
        // Arrange
        var inner = new Exception("Inner validation error");

        // Act
        var exception = new TestValidationExceptionWithInner("Validation failed", "VALIDATION_ERROR", inner);

        // Assert
        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
        Assert.Same(inner, exception.InnerException);
    }

    #endregion

    #region BusinessRuleException Tests

    [Fact]
    public void BusinessRuleException_WithMessage_SetsProperties()
    {
        // Arrange & Act
        var exception = new TestBusinessRuleException("Business rule violated", "BUSINESS_RULE_ERROR");

        // Assert
        Assert.Equal("Business rule violated", exception.Message);
        Assert.Equal("BUSINESS_RULE_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void BusinessRuleException_WithInnerException_SetsProperties()
    {
        // Arrange
        var inner = new Exception("Inner business error");

        // Act
        var exception = new TestBusinessRuleExceptionWithInner("Business rule violated", "BUSINESS_RULE_ERROR", inner);

        // Assert
        Assert.Equal("Business rule violated", exception.Message);
        Assert.Equal("BUSINESS_RULE_ERROR", exception.ErrorCode);
        Assert.Same(inner, exception.InnerException);
    }

    #endregion

    #region Document Exception Tests

    [Fact]
    public void DocumentNotFoundException_WithIntId_SetsProperties()
    {
        // Arrange & Act
        var exception = new DocumentNotFoundException(123);

        // Assert
        Assert.Contains("Document", exception.Message);
        Assert.Contains("123", exception.Message);
        Assert.Equal("Document", exception.EntityType);
        Assert.Equal(123, exception.EntityId);
        Assert.Equal("DOCUMENT_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void DocumentNotFoundException_WithGuid_SetsProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var exception = new DocumentNotFoundException(guid);

        // Assert
        Assert.Contains("Document", exception.Message);
        Assert.Contains(guid.ToString(), exception.Message);
        Assert.Equal("Document", exception.EntityType);
        Assert.Equal(guid, exception.EntityId);
        Assert.Equal("DOCUMENT_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void DocumentBindingNotFoundException_SetsProperties()
    {
        // Arrange & Act
        var exception = new DocumentBindingNotFoundException(456);

        // Assert
        Assert.Contains("DocumentBinding", exception.Message);
        Assert.Contains("456", exception.Message);
        Assert.Equal("DocumentBinding", exception.EntityType);
        Assert.Equal(456, exception.EntityId);
        Assert.Equal("BINDING_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void DocumentDeletedException_SetsProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var exception = new DocumentDeletedException(guid);

        // Assert
        Assert.Contains("deleted document", exception.Message);
        Assert.Contains(guid.ToString(), exception.Message);
        Assert.Equal(guid, exception.DocumentGuid);
        Assert.Equal("DOCUMENT_DELETED", exception.ErrorCode);
    }

    [Fact]
    public void DocumentInfectedException_SetsProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var exception = new DocumentInfectedException(guid);

        // Assert
        Assert.Contains("infected document", exception.Message);
        Assert.Contains(guid.ToString(), exception.Message);
        Assert.Equal(guid, exception.DocumentGuid);
        Assert.Equal("DOCUMENT_INFECTED", exception.ErrorCode);
    }

    [Fact]
    public void DocumentExpiredException_SetsProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var expiryDate = new DateTime(2023, 12, 31);

        // Act
        var exception = new DocumentExpiredException(guid, expiryDate);

        // Assert
        Assert.Contains("expired", exception.Message);
        Assert.Contains(guid.ToString(), exception.Message);
        Assert.Contains("2023-12-31", exception.Message);
        Assert.Equal(guid, exception.DocumentGuid);
        Assert.Equal(expiryDate, exception.ExpiryDate);
        Assert.Equal("DOCUMENT_EXPIRED", exception.ErrorCode);
    }

    [Fact]
    public void InvalidFileException_SetsProperties()
    {
        // Arrange & Act
        var exception = new InvalidFileException("test.pdf", "File is corrupted");

        // Assert
        Assert.Contains("test.pdf", exception.Message);
        Assert.Contains("File is corrupted", exception.Message);
        Assert.Equal("test.pdf", exception.FileName);
        Assert.Equal("File is corrupted", exception.Reason);
        Assert.Equal("INVALID_FILE", exception.ErrorCode);
    }

    [Fact]
    public void InvalidFileTypeException_SetsProperties()
    {
        // Arrange & Act
        var exception = new InvalidFileTypeException("test.exe", "application/exe", ".exe");

        // Assert
        Assert.Contains("test.exe", exception.Message);
        Assert.Contains("application/exe", exception.Message);
        Assert.Contains(".exe", exception.Message);
        Assert.Equal("test.exe", exception.FileName);
        Assert.Equal("application/exe", exception.ContentType);
        Assert.Equal(".exe", exception.FileExtension);
        Assert.Equal("INVALID_FILE_TYPE", exception.ErrorCode);
    }

    [Fact]
    public void FileSizeLimitExceededException_SetsProperties()
    {
        // Arrange & Act
        var exception = new FileSizeLimitExceededException("large.pdf", 150000000, 100000000);

        // Assert
        Assert.Contains("large.pdf", exception.Message);
        Assert.Contains("150000000", exception.Message);
        Assert.Contains("100000000", exception.Message);
        Assert.Equal("large.pdf", exception.FileName);
        Assert.Equal(150000000, exception.FileSize);
        Assert.Equal(100000000, exception.MaxFileSize);
        Assert.Equal("FILE_SIZE_EXCEEDED", exception.ErrorCode);
    }

    [Fact]
    public void FileStorageException_WithMessage_SetsProperties()
    {
        // Arrange & Act
        var exception = new FileStorageException("Storage failed");

        // Assert
        Assert.Equal("Storage failed", exception.Message);
        Assert.Null(exception.FilePath);
        Assert.Equal("FILE_STORAGE_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void FileStorageException_WithFilePath_SetsProperties()
    {
        // Arrange & Act
        var exception = new FileStorageException("Storage failed", "/uploads/test.pdf");

        // Assert
        Assert.Equal("Storage failed", exception.Message);
        Assert.Equal("/uploads/test.pdf", exception.FilePath);
        Assert.Equal("FILE_STORAGE_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void FileStorageException_WithInnerException_SetsProperties()
    {
        // Arrange
        var inner = new Exception("Inner storage error");

        // Act
        var exception = new FileStorageException("Storage failed", "/uploads/test.pdf", inner);

        // Assert
        Assert.Equal("Storage failed", exception.Message);
        Assert.Equal("/uploads/test.pdf", exception.FilePath);
        Assert.Same(inner, exception.InnerException);
        Assert.Equal("FILE_STORAGE_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void ChecksumMismatchException_SetsProperties()
    {
        // Arrange & Act
        var exception = new ChecksumMismatchException("test.pdf", "abc123", "def456");

        // Assert
        Assert.Contains("test.pdf", exception.Message);
        Assert.Contains("abc123", exception.Message);
        Assert.Contains("def456", exception.Message);
        Assert.Equal("test.pdf", exception.FileName);
        Assert.Equal("abc123", exception.ExpectedChecksum);
        Assert.Equal("def456", exception.ActualChecksum);
        Assert.Equal("CHECKSUM_MISMATCH", exception.ErrorCode);
    }

    #endregion

    #region Property Exception Tests

    [Fact]
    public void PropertyNotFoundException_SetsProperties()
    {
        // Arrange & Act
        var exception = new PropertyNotFoundException(789);

        // Assert
        Assert.Contains("Property", exception.Message);
        Assert.Contains("789", exception.Message);
        Assert.Equal("Property", exception.EntityType);
        Assert.Equal(789, exception.EntityId);
        Assert.Equal("PROPERTY_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void PropertyCertificateNotFoundException_SetsProperties()
    {
        // Arrange & Act
        var exception = new PropertyCertificateNotFoundException(321);

        // Assert
        Assert.Contains("PropertyCertificate", exception.Message);
        Assert.Contains("321", exception.Message);
        Assert.Equal("PropertyCertificate", exception.EntityType);
        Assert.Equal(321, exception.EntityId);
        Assert.Equal("PROPERTY_CERTIFICATE_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public void CertificateTypeNotFoundException_SetsProperties()
    {
        // Arrange & Act
        var exception = new CertificateTypeNotFoundException(654);

        // Assert
        Assert.Contains("CertificateType", exception.Message);
        Assert.Contains("654", exception.Message);
        Assert.Equal("CertificateType", exception.EntityType);
        Assert.Equal(654, exception.EntityId);
        Assert.Equal("CERTIFICATE_TYPE_NOT_FOUND", exception.ErrorCode);
    }

    #endregion

    #region Binding Exception Tests

    [Fact]
    public void InvalidBindingException_WithMessage_SetsProperties()
    {
        // Arrange & Act
        var exception = new InvalidBindingException("Invalid binding operation");

        // Assert
        Assert.Equal("Invalid binding operation", exception.Message);
        Assert.Null(exception.ModuleCode);
        Assert.Null(exception.ReferenceTableName);
        Assert.Equal("INVALID_BINDING", exception.ErrorCode);
    }

    [Fact]
    public void InvalidBindingException_WithModuleAndTable_SetsProperties()
    {
        // Arrange & Act
        var exception = new InvalidBindingException("Invalid binding", "PROPERTY", "PropertyCertificate");

        // Assert
        Assert.Equal("Invalid binding", exception.Message);
        Assert.Equal("PROPERTY", exception.ModuleCode);
        Assert.Equal("PropertyCertificate", exception.ReferenceTableName);
        Assert.Equal("INVALID_BINDING", exception.ErrorCode);
    }

    [Fact]
    public void DuplicateBindingException_SetsProperties()
    {
        // Arrange & Act
        var exception = new DuplicateBindingException(100, "PropertyCertificate", 200);

        // Assert
        Assert.Contains("100", exception.Message);
        Assert.Contains("PropertyCertificate", exception.Message);
        Assert.Contains("200", exception.Message);
        Assert.Equal(100, exception.DocumentId);
        Assert.Equal("PropertyCertificate", exception.ReferenceTableName);
        Assert.Equal(200, exception.ReferenceId);
        Assert.Equal("DUPLICATE_BINDING", exception.ErrorCode);
    }

    [Fact]
    public void XorValidationException_SetsProperties()
    {
        // Arrange & Act
        var exception = new XorValidationException("ReferenceTableId", "ReferenceTableIdGuid");

        // Assert
        Assert.Contains("ReferenceTableId", exception.Message);
        Assert.Contains("ReferenceTableIdGuid", exception.Message);
        Assert.Contains("Exactly one", exception.Message);
        Assert.Equal("ReferenceTableId", exception.Parameter1Name);
        Assert.Equal("ReferenceTableIdGuid", exception.Parameter2Name);
        Assert.Equal("XOR_VALIDATION_FAILED", exception.ErrorCode);
    }

    [Fact]
    public void PathTraversalException_SetsProperties()
    {
        // Arrange & Act
        var exception = new PathTraversalException("../../etc/passwd");

        // Assert
        Assert.Contains("../../etc/passwd", exception.Message);
        Assert.Contains("Path traversal", exception.Message);
        Assert.Equal("../../etc/passwd", exception.AttemptedPath);
        Assert.Equal("PATH_TRAVERSAL_DETECTED", exception.ErrorCode);
    }

    [Fact]
    public void InvalidFileNameException_SetsProperties()
    {
        // Arrange & Act
        var exception = new InvalidFileNameException("../file.pdf", "Contains path traversal characters");

        // Assert
        Assert.Contains("../file.pdf", exception.Message);
        Assert.Contains("Contains path traversal characters", exception.Message);
        Assert.Equal("../file.pdf", exception.FileName);
        Assert.Equal("INVALID_FILE_NAME", exception.ErrorCode);
    }

    #endregion

    #region Test Helper Classes

    private class TestNtisPlatformException : NtisPlatformException
    {
        public TestNtisPlatformException(string message, string errorCode) 
            : base(message, errorCode)
        {
        }
    }

    private class TestNtisPlatformExceptionWithInner : NtisPlatformException
    {
        public TestNtisPlatformExceptionWithInner(string message, string errorCode, Exception innerException) 
            : base(message, errorCode, innerException)
        {
        }
    }

    private class TestEntityNotFoundException : EntityNotFoundException
    {
        public TestEntityNotFoundException(string entityType, object entityId) 
            : base(entityType, entityId, "TEST_NOT_FOUND")
        {
        }
    }

    private class TestValidationException : ValidationException
    {
        public TestValidationException(string message, string errorCode) 
            : base(message, errorCode)
        {
        }
    }

    private class TestValidationExceptionWithInner : ValidationException
    {
        public TestValidationExceptionWithInner(string message, string errorCode, Exception innerException) 
            : base(message, errorCode, innerException)
        {
        }
    }

    private class TestBusinessRuleException : BusinessRuleException
    {
        public TestBusinessRuleException(string message, string errorCode) 
            : base(message, errorCode)
        {
        }
    }

    private class TestBusinessRuleExceptionWithInner : BusinessRuleException
    {
        public TestBusinessRuleExceptionWithInner(string message, string errorCode, Exception innerException) 
            : base(message, errorCode, innerException)
        {
        }
    }

    #endregion
}
