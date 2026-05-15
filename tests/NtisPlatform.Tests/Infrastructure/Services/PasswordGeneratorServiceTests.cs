using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class PasswordGeneratorServiceTests
{
    [Fact]
    public void Generate_Returns12CharacterPassword()
    {
        var service = new PasswordGeneratorService();

        var password = service.Generate();

        Assert.Equal(12, password.Length);
    }

    [Fact]
    public void Generate_ContainsAtLeastOneUppercase()
    {
        var service = new PasswordGeneratorService();
        for (var i = 0; i < 20; i++)
        {
            var password = service.Generate();
            Assert.Contains(password, c => char.IsUpper(c));
        }
    }

    [Fact]
    public void Generate_ContainsAtLeastOneLowercase()
    {
        var service = new PasswordGeneratorService();
        for (var i = 0; i < 20; i++)
        {
            var password = service.Generate();
            Assert.Contains(password, c => char.IsLower(c));
        }
    }

    [Fact]
    public void Generate_ContainsAtLeastOneDigit()
    {
        var service = new PasswordGeneratorService();
        for (var i = 0; i < 20; i++)
        {
            var password = service.Generate();
            Assert.Contains(password, c => char.IsDigit(c));
        }
    }

    [Fact]
    public void Generate_ContainsAtLeastOneSpecial()
    {
        const string specials = "!@#$%^&*-_=+?";
        var service = new PasswordGeneratorService();
        for (var i = 0; i < 20; i++)
        {
            var password = service.Generate();
            Assert.Contains(password, c => specials.Contains(c));
        }
    }

    [Fact]
    public void Generate_DoesNotContainAmbiguousCharacters()
    {
        // Confusables stripped from the pools: I, O, i, l, o, 0, 1
        const string ambiguous = "IOilo01";
        var service = new PasswordGeneratorService();
        for (var i = 0; i < 50; i++)
        {
            var password = service.Generate();
            foreach (var c in password)
            {
                Assert.DoesNotContain(c, ambiguous);
            }
        }
    }

    [Fact]
    public void Generate_ProducesUniquePasswords()
    {
        var service = new PasswordGeneratorService();
        var passwords = Enumerable.Range(0, 25).Select(_ => service.Generate()).ToHashSet();

        Assert.True(passwords.Count >= 24, "Generate should produce essentially unique passwords");
    }
}
