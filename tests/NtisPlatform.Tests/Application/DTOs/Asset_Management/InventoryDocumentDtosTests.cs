using System;
using NtisPlatform.Application.DTOs.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

public class InventoryDocumentDtosTests
{
    [Fact]
    public void InventoryDocumentUploadResponseDto_GetAndSetProperties_WorkCorrectly()
    {
        var guid = Guid.NewGuid();
        var dto = new InventoryDocumentUploadResponseDto
        {
            InventoryDocumentId = 1,
            DocumentGuid = guid,
            DocumentId = 10,
            DocumentBindingId = 100,
            InventoryBatchId = 5,
            DocumentTypeId = 2,
            DisplayOrder = 3,
            Remarks = "Test remark",
            FileName = "invoice.pdf",
            FileSizeBytes = 2048,
            StoragePath = "/storage/invoice.pdf"
        };

        Assert.Equal(1, dto.InventoryDocumentId);
        Assert.Equal(guid, dto.DocumentGuid);
        Assert.Equal(10, dto.DocumentId);
        Assert.Equal(100, dto.DocumentBindingId);
        Assert.Equal(5, dto.InventoryBatchId);
        Assert.Equal(2, dto.DocumentTypeId);
        Assert.Equal(3, dto.DisplayOrder);
        Assert.Equal("Test remark", dto.Remarks);
        Assert.Equal("invoice.pdf", dto.FileName);
        Assert.Equal(2048, dto.FileSizeBytes);
        Assert.Equal("/storage/invoice.pdf", dto.StoragePath);
    }

    [Fact]
    public void InventoryDocumentDto_GetAndSetProperties_WorkCorrectly()
    {
        var guid = Guid.NewGuid();
        var dto = new InventoryDocumentDto
        {
            InventoryDocumentId = 1,
            InventoryBatchId = 5,
            DocumentTypeId = 2,
            DocumentTypeCode = "CODE",
            DocumentTypeName = "Name",
            DisplayOrder = 1,
            Remarks = "Remark",
            DocumentBindingId = 50,
            DocumentGuid = guid,
            FileName = "file.png",
            MimeType = "image/png"
        };

        Assert.Equal(1, dto.InventoryDocumentId);
        Assert.Equal(5, dto.InventoryBatchId);
        Assert.Equal(2, dto.DocumentTypeId);
        Assert.Equal("CODE", dto.DocumentTypeCode);
        Assert.Equal("Name", dto.DocumentTypeName);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.Equal("Remark", dto.Remarks);
        Assert.Equal(50, dto.DocumentBindingId);
        Assert.Equal(guid, dto.DocumentGuid);
        Assert.Equal("file.png", dto.FileName);
        Assert.Equal("image/png", dto.MimeType);
    }
}
