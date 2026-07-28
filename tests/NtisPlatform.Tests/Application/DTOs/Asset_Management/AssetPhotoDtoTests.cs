using System;
using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

public class AssetPhotoDtoTests
{
    [Fact]
    public void AssetPhotoUploadResponseDto_PropertiesGetAndSetCorrectly()
    {
        var guid = Guid.NewGuid();
        var dto = new AssetPhotoUploadResponseDto
        {
            PhotoId = 1,
            DocumentGuid = guid,
            DocumentId = 10,
            DocumentBindingId = 20,
            AssetId = 30,
            PhotoTypeId = 2,
            DisplayOrder = 1,
            Remarks = "Remarks",
            FileName = "photo.jpg",
            FileSizeBytes = 1024,
            StoragePath = "/storage/photo.jpg"
        };

        Assert.Equal(1, dto.PhotoId);
        Assert.Equal(guid, dto.DocumentGuid);
        Assert.Equal(10, dto.DocumentId);
        Assert.Equal(20, dto.DocumentBindingId);
        Assert.Equal(30, dto.AssetId);
        Assert.Equal(2, dto.PhotoTypeId);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.Equal("Remarks", dto.Remarks);
        Assert.Equal("photo.jpg", dto.FileName);
        Assert.Equal(1024, dto.FileSizeBytes);
        Assert.Equal("/storage/photo.jpg", dto.StoragePath);
    }

    [Fact]
    public void AssetPhotoTypeWithStatusDto_PropertiesGetAndSetCorrectly()
    {
        var guid = Guid.NewGuid();
        var dto = new AssetPhotoTypeWithStatusDto
        {
            PhotoTypeId = 2,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front Elevation",
            DisplayOrder = 1,
            HasPhoto = true,
            PhotoCount = 3,
            PhotoId = 100,
            Remarks = "Remarks",
            DocumentBindingId = 50,
            DocumentGuid = guid,
            FileName = "front.jpg",
            MimeType = "image/jpeg"
        };

        Assert.Equal(2, dto.PhotoTypeId);
        Assert.Equal("FRONT", dto.PhotoTypeCode);
        Assert.Equal("Front Elevation", dto.PhotoTypeName);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.HasPhoto);
        Assert.Equal(3, dto.PhotoCount);
        Assert.Equal(100, dto.PhotoId);
        Assert.Equal("Remarks", dto.Remarks);
        Assert.Equal(50, dto.DocumentBindingId);
        Assert.Equal(guid, dto.DocumentGuid);
        Assert.Equal("front.jpg", dto.FileName);
        Assert.Equal("image/jpeg", dto.MimeType);
    }

    [Fact]
    public void AssetPhotoItemDto_PropertiesGetAndSetCorrectly()
    {
        var guid = Guid.NewGuid();
        var dto = new AssetPhotoItemDto
        {
            PhotoTypeId = 2,
            IsEnabled = true,
            DisplayOrder = 1,
            Remarks = "Remarks",
            ExistingPhotoId = 10,
            ExistingDocumentGuid = guid
        };

        Assert.Equal(2, dto.PhotoTypeId);
        Assert.True(dto.IsEnabled);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.Equal("Remarks", dto.Remarks);
        Assert.Equal(10, dto.ExistingPhotoId);
        Assert.Equal(guid, dto.ExistingDocumentGuid);
    }

    [Fact]
    public void AssetPhotoBulkSaveResponseDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new AssetPhotoBulkSaveResponseDto
        {
            AssetId = 10,
            TotalProcessed = 5,
            EnabledCount = 3,
            DisabledCount = 2,
            UpdatedPhotoTypes = new List<AssetPhotoTypeWithStatusDto>(),
            Errors = new List<string> { "Error 1" }
        };

        Assert.Equal(10, dto.AssetId);
        Assert.Equal(5, dto.TotalProcessed);
        Assert.Equal(3, dto.EnabledCount);
        Assert.Equal(2, dto.DisabledCount);
        Assert.Empty(dto.UpdatedPhotoTypes);
        Assert.Single(dto.Errors);
    }
}
