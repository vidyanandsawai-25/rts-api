using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class PasswordGeneratorServiceTests
{
    private readonly PasswordGeneratorService _service;

    public PasswordGeneratorServiceTests()
    {
        _service = new PasswordGeneratorService();
    }

    [Fact]
    public void Generate_ReturnsPasswordWithCorrectLength()
    {
        // Act
        var password = _service.Generate();

        // Assert
        Assert.Equal(12, password.Length);
    }

    [Fact]
    public void Generate_ContainsAtLeastOneUppercaseLetter()
    {
        // Act
        var password = _service.Generate();

        // Assert
        Assert.Contains(password, char.IsUpper);
    }

    [Fact]
    public void Generate_ContainsAtLeastOneLowercaseLetter()
    {
        // Act
        var password = _service.Generate();

        // Assert
        Assert.Contains(password, char.IsLower);
    }

    [Fact]
    public void Generate_ContainsAtLeastOneDigit()
    {
        // Act
        var password = _service.Generate();

        // Assert
        Assert.Contains(password, char.IsDigit);
    }

    [Fact]
    public void Generate_ContainsAtLeastOneSpecialCharacter()
    {
        // Act
        var password = _service.Generate();

        // Assert
        var specialChars = "!@#$%^&*-_=+?";
        Assert.Contains(password, c => specialChars.Contains(c));
    }

    [Fact]
    public void Generate_DoesNotContainAmbiguousCharacters()
    {
        // Act
        var passwords = Enumerable.Range(0, 100).Select(_ => _service.Generate()).ToList();

        // Assert - Should not contain I, O, l, 0, 1 (ambiguous characters)
        var ambiguousChars = "IOl01";
        foreach (var password in passwords)
        {
            foreach (var ambiguous in ambiguousChars)
            {
                Assert.DoesNotContain(ambiguous, password);
            }
        }
    }

    [Fact]
    public void Generate_OnlyUsesAllowedCharacters()
    {
        // Arrange
        var allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%^&*-_=+?";

        // Act
        var passwords = Enumerable.Range(0, 50).Select(_ => _service.Generate()).ToList();

        // Assert
        foreach (var password in passwords)
        {
            foreach (var c in password)
            {
                Assert.Contains(c, allowedChars);
            }
        }
    }

    [Fact]
    public void Generate_ProducesVariedPasswords()
    {
        // Act - Generate multiple passwords
        var passwords = Enumerable.Range(0, 100).Select(_ => _service.Generate()).ToList();
        var distinctPasswords = passwords.Distinct().Count();

        // Assert - Sanity check that generation is not returning the same password every time
        Assert.True(distinctPasswords > 1, "Generated passwords should show some variation across multiple runs.");
    }

    [Fact]
    public void Generate_MeetsComplexityRequirements()
    {
        // Act
        var passwords = Enumerable.Range(0, 50).Select(_ => _service.Generate()).ToList();

        // Assert
        foreach (var password in passwords)
        {
            Assert.True(password.Length == 12, $"Password length should be 12 but was {password.Length}");
            Assert.True(password.Any(char.IsUpper), "Password should contain at least one uppercase letter");
            Assert.True(password.Any(char.IsLower), "Password should contain at least one lowercase letter");
            Assert.True(password.Any(char.IsDigit), "Password should contain at least one digit");

            var specialChars = "!@#$%^&*-_=+?";
            Assert.True(password.Any(c => specialChars.Contains(c)), "Password should contain at least one special character");
        }
    }

    [Fact]
    public void Generate_HasGoodRandomDistribution()
    {
        // Act - Generate many passwords
        var passwords = Enumerable.Range(0, 1000).Select(_ => _service.Generate()).ToList();

        // Assert - Check character type distribution
        var upperCount = passwords.Sum(p => p.Count(char.IsUpper));
        var lowerCount = passwords.Sum(p => p.Count(char.IsLower));
        var digitCount = passwords.Sum(p => p.Count(char.IsDigit));

        var specialChars = "!@#$%^&*-_=+?";
        var specialCount = passwords.Sum(p => p.Count(c => specialChars.Contains(c)));

        // Each password has 12 chars, so total chars = 12000
        // With good distribution, each type should appear multiple times
        Assert.True(upperCount >= 1000, "Upper case letters should appear at least 1000 times");
        Assert.True(lowerCount >= 1000, "Lower case letters should appear at least 1000 times");
        Assert.True(digitCount >= 1000, "Digits should appear at least 1000 times");
        Assert.True(specialCount >= 1000, "Special characters should appear at least 1000 times");
    }

    [Fact]
    public void Generate_IsNotPredictable()
    {
        // Act - Generate two batches of passwords
        var batch1 = Enumerable.Range(0, 10).Select(_ => _service.Generate()).ToList();
        var batch2 = Enumerable.Range(0, 10).Select(_ => _service.Generate()).ToList();

        // Assert - Batches should have no overlap
        var intersection = batch1.Intersect(batch2).ToList();
        Assert.Empty(intersection);
    }

    [Fact]
    public void Generate_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        for (int i = 0; i < 1000; i++)
        {
            var password = _service.Generate();
            Assert.NotNull(password);
            Assert.Equal(12, password.Length);
        }
    }

    [Fact]
    public void Generate_EachPositionCanHaveAnyCharacter()
    {
        // Act - Generate many passwords to check position variance
        var passwords = Enumerable.Range(0, 100).Select(_ => _service.Generate()).ToList();

        // Assert - First 4 positions should have variety (not always same character type)
        var firstCharTypes = passwords.Select(p => GetCharType(p[0])).Distinct().ToList();
        var secondCharTypes = passwords.Select(p => GetCharType(p[1])).Distinct().ToList();

        // After shuffling, any position can have any character type
        // We expect to see variety across many generations
        Assert.True(firstCharTypes.Count > 1 || secondCharTypes.Count > 1, 
            "Shuffling should distribute character types across positions");
    }

    [Fact]
    public void Generate_GuaranteesAllRequiredCharacterTypes()
    {
        // Act - Generate multiple passwords
        var passwords = Enumerable.Range(0, 100).Select(_ => _service.Generate()).ToList();

        // Assert - Every password must have all 4 character types
        var specialChars = "!@#$%^&*-_=+?";

        foreach (var password in passwords)
        {
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => specialChars.Contains(c));

            Assert.True(hasUpper && hasLower && hasDigit && hasSpecial,
                $"Password '{password}' missing required character types. " +
                $"Upper:{hasUpper}, Lower:{hasLower}, Digit:{hasDigit}, Special:{hasSpecial}");
        }
    }

    private string GetCharType(char c)
    {
        if (char.IsUpper(c)) return "Upper";
        if (char.IsLower(c)) return "Lower";
        if (char.IsDigit(c)) return "Digit";
        return "Special";
    }
}
