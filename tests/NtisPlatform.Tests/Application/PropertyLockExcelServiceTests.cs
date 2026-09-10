using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using System.IO;

namespace NtisPlatform.Tests.Application;

public class PropertyLockExcelServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<PropertyLockExcelService>> _mockLogger;
    private readonly PropertyLockExcelService _service;

    public PropertyLockExcelServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<PropertyLockExcelService>>();

        var wardRepoMock = new Mock<IRepository<WardEntity>>();
        var zoneRepoMock = new Mock<IRepository<ZoneEntity>>();
        var propertyRepoMock = new Mock<IRepository<PropertyEntity>>();
        var propertyScreenLockRepoMock = new Mock<IRepository<PropertyScreenLockEntity>>();
        var screenMasterRepoMock = new Mock<IRepository<ScreenMasterEntity>>();

        wardRepoMock.Setup(r => r.GetQueryable()).Returns(_context.WardMaster);
        zoneRepoMock.Setup(r => r.GetQueryable()).Returns(_context.ZoneMaster);
        propertyRepoMock.Setup(r => r.GetQueryable()).Returns(_context.PropertyMast);
        propertyScreenLockRepoMock.Setup(r => r.GetQueryable()).Returns(_context.PropertyScreenLocks);
        screenMasterRepoMock.Setup(r => r.GetQueryable()).Returns(_context.ScreenMaster);

        _service = new PropertyLockExcelService(
            wardRepoMock.Object,
            zoneRepoMock.Object,
            propertyRepoMock.Object,
            propertyScreenLockRepoMock.Object,
            screenMasterRepoMock.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed Zone data
        var zone = new ZoneEntity { Id = 1, ZoneNo = "Z001", Description = "Zone 1", IsActive = true };
        _context.ZoneMaster.Add(zone);

        // Seed Ward data
        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, Description = "Ward 1", IsActive = true };
        _context.WardMaster.Add(ward);

        // Seed PropertyMast data
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, PropertyNo = "P001", PartitionNo = "A", WardId = 1, IsActive = true },
            new() { Id = 2, PropertyNo = "P002", PartitionNo = "B", WardId = 1, IsActive = true },
            new() { Id = 3, PropertyNo = "P003", PartitionNo = "C", WardId = 1, IsActive = true },
        };
        _context.PropertyMast.AddRange(properties);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private Stream CreateMockExcelStream(List<(string? zone, string? ward, string? propertyNo, string? partitionNo)> rows)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "ZoneNo";
        worksheet.Cell(1, 2).Value = "WardNo";
        worksheet.Cell(1, 3).Value = "PropertyNo";
        worksheet.Cell(1, 4).Value = "PartitionNo";

        for (int i = 0; i < rows.Count; i++)
        {
            worksheet.Cell(i + 2, 1).Value = rows[i].zone ?? string.Empty;
            worksheet.Cell(i + 2, 2).Value = rows[i].ward ?? string.Empty;
            worksheet.Cell(i + 2, 3).Value = rows[i].propertyNo ?? string.Empty;
            worksheet.Cell(i + 2, 4).Value = rows[i].partitionNo ?? string.Empty;
        }

        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    [Fact]
    public async Task GetPropertyLocksByExcelFileAsync_ThrowsArgumentException_WhenFileStreamIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetPropertyLocksByExcelFileAsync(null!, 1, 10, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetPropertyLocksByExcelFileAsync_ThrowsArgumentException_WhenMissingFixedColumns()
    {
        // Arrange - Excel missing PartitionNo column
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        worksheet.Cell(1, 1).Value = "ZoneNo";
        worksheet.Cell(1, 2).Value = "WardNo";
        worksheet.Cell(1, 3).Value = "PropertyNo";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetPropertyLocksByExcelFileAsync(stream, 1, 10, null, CancellationToken.None));

        Assert.Contains("Missing required column(s): PartitionNo", ex.Message);
    }

    [Fact]
    public async Task GetPropertyLocksByExcelFileAsync_ReturnsMatchedProperties_WhenValidExcelProvided()
    {
        // Arrange
        var rows = new List<(string?, string?, string?, string?)>
        {
            ("Z001", "W001", "P001", "A"),
            ("Z001", "W001", "P002", "B")
        };
        using var stream = CreateMockExcelStream(rows);

        // Act
        var result = await _service.GetPropertyLocksByExcelFileAsync(stream, 1, 10, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, p => p.PropertyNo == "P001" && p.PartitionNo == "A");
        Assert.Contains(result.Items, p => p.PropertyNo == "P002" && p.PartitionNo == "B");
    }

    [Fact]
    public async Task GetPropertyLocksByExcelRowsAsync_WithDuplicates_ReturnsCorrectDuplicateCount()
    {
        // Arrange: 4 rows total with 3 duplicates of P001-A and 1 of P002-B
        var request = new SearchByExcelRequestDto
        {
            PageNumber = 1,
            PageSize = 10,
            Rows = new List<ExcelPropertyRow>
            {
                new() { ZoneNo = "Z001", WardNo = "W001", PropertyNo = "P001", PartitionNo = "A" },
                new() { ZoneNo = "Z001", WardNo = "W001", PropertyNo = "P001", PartitionNo = "A" },
                new() { ZoneNo = "Z001", WardNo = "W001", PropertyNo = "P001", PartitionNo = "A" },
                new() { ZoneNo = "Z001", WardNo = "W001", PropertyNo = "P002", PartitionNo = "B" }
            }
        };

        // Act
        var result = await _service.GetPropertyLocksByExcelRowsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.DublicateCount);
    }
}
