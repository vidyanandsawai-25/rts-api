using System.Security.Cryptography;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

public class PasswordGeneratorService : IPasswordGeneratorService
{
    private const int Length = 12;
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*-_=+?";
    private const string All = Upper + Lower + Digits + Special;

    public string Generate()
    {
        var buffer = new char[Length];

        // Guarantee at least one from each required group
        buffer[0] = Pick(Upper);
        buffer[1] = Pick(Lower);
        buffer[2] = Pick(Digits);
        buffer[3] = Pick(Special);

        for (var i = 4; i < Length; i++)
            buffer[i] = Pick(All);

        Shuffle(buffer);
        return new string(buffer);
    }

    private static char Pick(string source)
        => source[RandomNumberGenerator.GetInt32(source.Length)];

    private static void Shuffle(char[] buffer)
    {
        for (var i = buffer.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }
    }
}