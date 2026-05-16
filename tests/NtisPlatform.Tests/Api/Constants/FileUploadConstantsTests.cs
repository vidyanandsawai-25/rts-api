using NtisPlatform.Api.Constants;
using Xunit;

namespace NtisPlatform.Tests.Api.Constants;

public class FileUploadConstantsTests
{
    [Fact]
    public void MaxFileSizeBytes_HasCorrectValue()
    {
        // Assert
        Assert.Equal(104857600, FileUploadConstants.MaxFileSizeBytes);
    }

    [Fact]
    public void MaxFileSizeBytes_Equals100MB()
    {
        // Arrange
        const long expectedBytes = 100 * 1024 * 1024; // 100 MB

        // Assert
        Assert.Equal(expectedBytes, FileUploadConstants.MaxFileSizeBytes);
    }

    [Fact]
    public void MaxFileSizeBytes_IsPositive()
    {
        // Assert
        Assert.True(FileUploadConstants.MaxFileSizeBytes > 0);
    }
}
