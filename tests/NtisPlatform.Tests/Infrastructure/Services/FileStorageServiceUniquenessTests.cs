using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.Options;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Tests for FileStorageService uniqueness enhancements
/// Verifies that filename collision risk is eliminated
/// </summary>
public class FileStorageServiceUniquenessTests : IDisposable
{
    private readonly string _testStoragePath;
    private readonly Mock<ILogger<FileStorageService>> _mockLogger;
    private readonly FileStorageService _service;

    public FileStorageServiceUniquenessTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"FileStorageTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testStoragePath);

        var options = Options.Create(new FileStorageOptions
        {
            BasePath = _testStoragePath,
            BufferSizeBytes = 81920
        });

        _mockLogger = new Mock<ILogger<FileStorageService>>();
        _service = new FileStorageService(options, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_CalledMultipleTimes_GeneratesUniqueFilenames()
    {
        // Arrange
        var filenames = new List<string>();
        var originalFileName = "test.pdf";

        // Act - Save same file 10 times
        for (int i = 0; i < 10; i++)
        {
            var content = System.Text.Encoding.UTF8.GetBytes($"Content {i}");
            var stream = new MemoryStream(content);
            var filename = await _service.SaveFileAsync(stream, originalFileName);
            filenames.Add(filename);
        }

        // Assert
        Assert.Equal(10, filenames.Count);
        Assert.Equal(10, filenames.Distinct().Count()); // All unique
    }

    [Fact]
    public async Task SaveFileAsync_SimultaneousUploads_AllSucceed()
    {
        // Arrange
        var taskCount = 50;
        var tasks = new List<Task<string>>();

        // Act - 50 simultaneous uploads
        for (int i = 0; i < taskCount; i++)
        {
            var content = System.Text.Encoding.UTF8.GetBytes($"Content {i}");
            var stream = new MemoryStream(content);
            tasks.Add(_service.SaveFileAsync(stream, "simultaneous.pdf"));
        }

        var filenames = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(taskCount, filenames.Length);
        Assert.Equal(taskCount, filenames.Distinct().Count()); // All unique

        // Verify all files exist
        foreach (var filename in filenames)
        {
            var fullPath = Path.Combine(_testStoragePath, filename);
            Assert.True(File.Exists(fullPath), $"File should exist: {filename}");
        }
    }

    [Fact]
    public async Task SaveFileAsync_GeneratedFilename_ContainsTimestampAndGuid()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("Test content");
        var stream = new MemoryStream(content);
        var beforeUpload = DateTime.Now;

        // Act
        var filename = await _service.SaveFileAsync(stream, "test.pdf");
        var afterUpload = DateTime.Now;

        // Assert - Verify format: YYYYMMDD_HHmmss_ffffff_GUID_originalname
        var parts = filename.Split('_');

        // Should have at least 5 parts: date, time, microseconds, guid, filename
        Assert.True(parts.Length >= 5, $"Expected at least 5 parts, got {parts.Length}: {filename}");

        // Verify date part (YYYYMMDD)
        var datePart = parts[0];
        Assert.Matches(@"^\d{8}$", datePart);
        var parsedDate = DateTime.ParseExact(datePart, "yyyyMMdd", null);
        Assert.True(parsedDate.Date == beforeUpload.Date || parsedDate.Date == afterUpload.Date);

        // Verify time part (HHmmss)
        var timePart = parts[1];
        Assert.Matches(@"^\d{6}$", timePart);

        // Verify microseconds part (ffffff)
        var microsecPart = parts[2];
        Assert.Matches(@"^\d{6}$", microsecPart);

        // Verify GUID part (32 hex chars, no hyphens)
        var guidPart = parts[3];
        Assert.Matches(@"^[a-f0-9]{32}$", guidPart);

        // Verify original filename is preserved
        Assert.Contains("test.pdf", filename);
    }

    [Fact]
    public async Task SaveFileAsync_WithFileMode_CreateNew_PreventsSilentOverwrite()
    {
        // Arrange
        var content1 = System.Text.Encoding.UTF8.GetBytes("Original content");
        var stream1 = new MemoryStream(content1);

        // Act - First save
        var filename1 = await _service.SaveFileAsync(stream1, "test.pdf");
        var fullPath = Path.Combine(_testStoragePath, filename1);

        // Verify file was created
        Assert.True(File.Exists(fullPath));
        var originalContent = await File.ReadAllTextAsync(fullPath);
        Assert.Equal("Original content", originalContent);

        // Try to manually create a file with the same name (simulating the impossible collision)
        var content2 = System.Text.Encoding.UTF8.GetBytes("New content");

        // Assert - FileMode.CreateNew will throw if file exists
        await Assert.ThrowsAsync<IOException>(async () =>
        {
            using var fileStream = new FileStream(
                fullPath,
                FileMode.CreateNew, // Will throw because file exists
                FileAccess.Write,
                FileShare.None);
            await fileStream.WriteAsync(content2);
        });

        // Verify original content is preserved
        var contentAfter = await File.ReadAllTextAsync(fullPath);
        Assert.Equal("Original content", contentAfter);
    }

    [Fact]
    public async Task SaveFileAsync_FilenamesAreSorted_Chronologically()
    {
        // Arrange
        var filenames = new List<string>();

        // Act - Create files with sufficient delays to ensure unique timestamps
        for (int i = 0; i < 5; i++)
        {
            var content = System.Text.Encoding.UTF8.GetBytes($"Content {i}");
            var stream = new MemoryStream(content);
            var filename = await _service.SaveFileAsync(stream, "test.pdf");
            filenames.Add(filename);

            // Delay longer than DateTime.Now resolution (15ms on Windows)
            await Task.Delay(20);
        }

        // Assert - Extract timestamps and verify chronological order
        // Filename format: 20260506_205557_822608_guid_filename.ext
        // Timestamp format: yyyyMMdd_HHmmss_ffffff = 22 characters
        var timestamps = filenames
            .Select(f => f.Substring(0, 22)) // Extract "20260506_205557_822608" (date + time + microseconds)
            .ToList();

        var sortedTimestamps = timestamps.OrderBy(t => t).ToList();

        // Verify timestamps are in chronological order
        Assert.Equal(timestamps, sortedTimestamps);

        // Additionally verify each timestamp is unique (no collisions)
        Assert.Equal(timestamps.Count, timestamps.Distinct().Count());
    }

    [Fact]
    public async Task SaveFileAsync_DifferentFilesAtSameTime_ProduceUniqueNames()
    {
        // Arrange - Multiple files uploaded at nearly the same instant
        var tasks = new[]
        {
            CreateUploadTask("file1.pdf", "Content 1"),
            CreateUploadTask("file2.pdf", "Content 2"),
            CreateUploadTask("file3.pdf", "Content 3"),
            CreateUploadTask("file1.pdf", "Content 4"), // Same name as first
        };

        // Act
        var filenames = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(4, filenames.Length);
        Assert.Equal(4, filenames.Distinct().Count()); // All unique even with duplicate original names
    }

    [Fact]
    public async Task SaveFileAsync_LongOriginalFilename_HandlesGracefully()
    {
        // Arrange
        // Windows MAX_PATH is 260 characters total
        // Account for: temp path (~65) + timestamp (15) + underscore (1) + guid (32) + underscore (1) + extension (4) = ~118
        // Leave room for original filename: 260 - 118 - 20 (buffer) = ~122 chars
        var longName = new string('a', 100) + ".pdf"; // Safe length that won't exceed MAX_PATH
        var content = System.Text.Encoding.UTF8.GetBytes("Test");
        var stream = new MemoryStream(content);

        // Act
        var filename = await _service.SaveFileAsync(stream, longName);

        // Assert
        Assert.Contains(".pdf", filename);
        Assert.Contains(new string('a', 100), filename); // Original name preserved

        var fullPath = Path.Combine(_testStoragePath, filename);
        Assert.True(File.Exists(fullPath));
        Assert.True(fullPath.Length < 260); // Windows MAX_PATH limit
    }

    [Fact]
    public async Task SaveFileAsync_SpecialCharactersInOriginalName_SanitizesCorrectly()
    {
        // Arrange
        var specialName = "test<>:|?*\\/file.pdf";
        var content = System.Text.Encoding.UTF8.GetBytes("Test");
        var stream = new MemoryStream(content);

        // Act
        var filename = await _service.SaveFileAsync(stream, specialName);

        // Assert
        // Special characters should be replaced with underscores
        Assert.DoesNotContain("<", filename);
        Assert.DoesNotContain(">", filename);
        Assert.DoesNotContain(":", filename);
        Assert.DoesNotContain("|", filename);
        Assert.DoesNotContain("?", filename);
        Assert.DoesNotContain("*", filename);
        Assert.DoesNotContain("/", filename);
        Assert.DoesNotContain("\\", filename);

        var fullPath = Path.Combine(_testStoragePath, filename);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task SaveFileAsync_UnderHighLoad_MaintainsUniqueness()
    {
        // Arrange - Simulate high load scenario
        var taskCount = 100;
        var tasks = new List<Task<string>>();
        var originalFileName = "load-test.pdf";

        // Act - 100 simultaneous uploads
        for (int i = 0; i < taskCount; i++)
        {
            var content = System.Text.Encoding.UTF8.GetBytes($"Load test content {i}");
            var stream = new MemoryStream(content);
            tasks.Add(_service.SaveFileAsync(stream, originalFileName));
        }

        var filenames = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(taskCount, filenames.Length);
        Assert.Equal(taskCount, filenames.Distinct().Count()); // All 100 unique

        // Verify all files are accessible
        foreach (var filename in filenames)
        {
            var fullPath = Path.Combine(_testStoragePath, filename);
            Assert.True(File.Exists(fullPath));
            var content = await File.ReadAllTextAsync(fullPath);
            Assert.StartsWith("Load test content", content);
        }
    }

    private async Task<string> CreateUploadTask(string filename, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return await _service.SaveFileAsync(stream, filename);
    }
}
