using NtisPlatform.Core.Constants;
using Xunit;

namespace NtisPlatform.Tests.Core.Constants;

public class HttpContextKeysTests
{
    [Fact]
    public void CurrentLanguage_HasCorrectValue()
    {
        // Assert
        Assert.Equal("CurrentLanguage", HttpContextKeys.CurrentLanguage);
    }

    [Fact]
    public void CurrentLanguage_IsNotNullOrEmpty()
    {
        // Assert
        Assert.False(string.IsNullOrEmpty(HttpContextKeys.CurrentLanguage));
    }

    [Fact]
    public void CurrentLanguage_IsString()
    {
        // Assert
        Assert.IsType<string>(HttpContextKeys.CurrentLanguage);
    }
}
