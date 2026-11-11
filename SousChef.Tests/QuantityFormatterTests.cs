using SousChef.Core.Cooking;

namespace SousChef.Tests;

/// <summary>
/// Tests for formatting quantities.
/// </summary>
public class QuantityFormatterTests
{
    /// <summary>
    /// Quantities are formatted with fractions for counts and spoons, decimals for weights, and plural units.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unit">The card unit.</param>
    /// <param name="expected">The expected text.</param>
    [Theory]
    [InlineData(150, "g", "150 g")]
    [InlineData(22.5, "g", "22.5 g")]
    [InlineData(0.5, "unit(s)", "½")]
    [InlineData(1.5, "tbsp", "1½ tbsp")]
    [InlineData(1, "sachet(s)", "1 sachet")]
    [InlineData(2, "sachet(s)", "2 sachets")]
    [InlineData(0.25, "unit(s)", "¼")]
    [InlineData(0.3, "tbsp", "0.3 tbsp")]
    public void Format_FormatsQuantity(decimal quantity, string unit, string expected)
    {
        Assert.Equal(expected, QuantityFormatter.Format(quantity, unit));
    }

    /// <summary>
    /// Inline amounts mark plain counts with a multiplication sign.
    /// </summary>
    [Fact]
    public void FormatAmount_MarksPlainCounts()
    {
        Assert.Equal("×2", QuantityFormatter.FormatAmount(2, "unit(s)"));
        Assert.Equal("15 g", QuantityFormatter.FormatAmount(15, "g"));
        Assert.Equal(string.Empty, QuantityFormatter.FormatAmount(null, "g"));
    }
}