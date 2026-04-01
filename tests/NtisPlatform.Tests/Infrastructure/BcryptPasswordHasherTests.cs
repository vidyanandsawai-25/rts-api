using NtisPlatform.Infrastructure.Services;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Unit tests for BcryptPasswordHasher
/// Tests password hashing and verification
/// </summary>
public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _passwordHasher;

    public BcryptPasswordHasherTests()
    {
        _passwordHasher = new BcryptPasswordHasher();
    }

    #region HashPassword Tests

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsHash()
    {
        // Arrange
        string password = "MySecurePassword123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_GeneratesDifferentHashes()
    {
        // Arrange
        string password = "MySecurePassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Bcrypt generates unique salts
    }

    [Fact]
    public void HashPassword_GeneratesValidBcryptHash()
    {
        // Arrange
        string password = "TestPassword123";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        // Bcrypt hashes start with $2a$, $2b$, or $2y$
        Assert.Matches(@"^\$2[aby]\$\d{2}\$.{53}$", hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ShortPwd")]
    [InlineData("VeryLongPasswordWithManyCharacters1234567890!@#$%^&*()")]
    public void HashPassword_WithVariousLengths_ReturnsValidHash(string password)
    {
        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Matches(@"^\$2[aby]\$\d{2}\$.{53}$", hash);
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        string password = "MySecurePassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        string correctPassword = "CorrectPassword123";
        string wrongPassword = "WrongPassword456";
        var hash = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        string password = "MyPassword123";
        string differentCasePassword = "mypassword123";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var correctResult = _passwordHasher.VerifyPassword(password, hash);
        var incorrectResult = _passwordHasher.VerifyPassword(differentCasePassword, hash);

        // Assert
        Assert.True(correctResult);
        Assert.False(incorrectResult);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        string password = "TestPassword123";
        string invalidHash = "not-a-valid-bcrypt-hash";

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ReturnsFalse()
    {
        // Arrange
        string password = "TestPassword123";
        string emptyHash = "";

        // Act
        var result = _passwordHasher.VerifyPassword(password, emptyHash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ReturnsFalse()
    {
        // Arrange
        string password = "TestPassword123";
        string malformedHash = "$2a$12$invalidhashformat";

        // Act
        var result = _passwordHasher.VerifyPassword(password, malformedHash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("P@ssw0rd!")]
    [InlineData("SimplePass")]
    [InlineData("VeryComplexP@ssw0rd123!@#")]
    public void VerifyPassword_WithVariousPasswords_WorksCorrectly(string password)
    {
        // Arrange
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var correctResult = _passwordHasher.VerifyPassword(password, hash);
        var incorrectResult = _passwordHasher.VerifyPassword(password + "wrong", hash);

        // Assert
        Assert.True(correctResult);
        Assert.False(incorrectResult);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void HashAndVerify_MultiplePasswords_WorksCorrectly()
    {
        // Arrange
        var passwords = new[]
        {
            "Password1",
            "Password2",
            "Password3",
            "Admin@123",
            "User@456"
        };

        // Act & Assert
        foreach (var password in passwords)
        {
            var hash = _passwordHasher.HashPassword(password);
            
            // Correct password should verify
            Assert.True(_passwordHasher.VerifyPassword(password, hash));
            
            // Incorrect password should not verify
            Assert.False(_passwordHasher.VerifyPassword(password + "wrong", hash));
        }
    }

    [Fact]
    public void Bcrypt_UsesWorkFactor12()
    {
        // Arrange
        string password = "TestPassword123";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        // Bcrypt hash format: $2a$12$... where 12 is the work factor
        Assert.Contains("$2a$12$", hash);
    }

    #endregion
}
