using NtisPlatform.Application.Utilities;
using Xunit;

namespace NtisPlatform.Tests.Application.Utilities;

public class NaturalStringComparerTests
{
    private readonly NaturalStringComparer _comparer = NaturalStringComparer.Instance;

    [Fact]
    public void Compare_NullValues_ReturnsExpected()
    {
        Assert.Equal(0, _comparer.Compare(null, null));
        Assert.True(_comparer.Compare(null, "A") < 0);
        Assert.True(_comparer.Compare("A", null) > 0);
    }

    [Fact]
    public void Compare_EmptyStrings_ReturnsExpected()
    {
        Assert.Equal(0, _comparer.Compare(string.Empty, string.Empty));
        Assert.True(_comparer.Compare(string.Empty, "A") < 0);
        Assert.True(_comparer.Compare("A", string.Empty) > 0);
    }

    [Fact]
    public void Compare_SimpleNumericNaturalSorting_ReturnsExpected()
    {
        Assert.True(_comparer.Compare("1", "2") < 0);
        Assert.True(_comparer.Compare("2", "10") < 0);
        Assert.True(_comparer.Compare("A1", "A2") < 0);
        Assert.True(_comparer.Compare("A2", "A10") < 0);
        Assert.True(_comparer.Compare("A1B", "A2B") < 0);
        Assert.True(_comparer.Compare("A2B", "A10B") < 0);
    }

    [Fact]
    public void Compare_LargeNumbersExceedingInt32MaxValue_SortsNumerically()
    {
        // "2147483648" is 2^31, which exceeds Int32.MaxValue (2147483647)
        // "100000000000" is 10^11.
        // Numerically, 2147483648 < 100000000000.
        // Lexicographically, '2' > '1', so if parsing fails it falls back to lexicographical and says "2147483648" > "100000000000".
        // Natural sorting should correctly identify 2147483648 < 100000000000.
        Assert.True(_comparer.Compare("A2147483648", "A100000000000") < 0);
    }

    [Fact]
    public void Compare_LeadingZeroes_ReturnsNonZeroDeterministic()
    {
        // "A1" vs "A01" vs "A001"
        // They represent the same numeric value but are distinct strings.
        // The comparison should be stable and non-zero so they are not treated as duplicate keys.
        int cmp1 = _comparer.Compare("A1", "A01");
        int cmp2 = _comparer.Compare("A01", "A001");
        int cmp3 = _comparer.Compare("A1", "A001");

        Assert.NotEqual(0, cmp1);
        Assert.NotEqual(0, cmp2);
        Assert.NotEqual(0, cmp3);
    }

    [Fact]
    public void Compare_CaseInsensitiveWithDeterministicTieBreaker_ReturnsExpected()
    {
        // "A" and "a" should not compare as equal, though case-insensitive comparison would treat them so.
        int cmp = _comparer.Compare("A", "a");
        Assert.NotEqual(0, cmp);
    }
}
