using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Tests;

/// <summary>
/// Tests for scaling quantities to a number of people.
/// </summary>
public class ServingScalerTests
{
    private static readonly Ingredient PakChoi = new() { Name = "Pak Choi", Unit = "unit(s)", Quantities = new() { [2] = 1, [3] = 2, [4] = 2 } };

    /// <summary>
    /// Serving counts on the card use the card's column, and others add columns together.
    /// </summary>
    /// <param name="servings">The number of people.</param>
    /// <param name="expected">The expected quantity.</param>
    [Theory]
    [InlineData(1, 0.5)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    [InlineData(7, 4)]
    [InlineData(8, 4)]
    public void Scale_UsesCardColumns(int servings, decimal expected)
    {
        Assert.Equal(expected, ServingScaler.Scale(PakChoi, servings));
    }

    /// <summary>
    /// Combinations use the fewest columns, preferring larger ones.
    /// </summary>
    [Fact]
    public void GetCombination_PrefersFewestThenLargest()
    {
        int[] sizes = [2, 3, 4];
        Assert.Equal([4, 2], ServingScaler.GetCombination(sizes, 6));
        Assert.Equal([4, 3], ServingScaler.GetCombination(sizes, 7));
        Assert.Equal([4, 4], ServingScaler.GetCombination(sizes, 8));
        Assert.Equal([3, 2], ServingScaler.GetCombination(sizes, 5));
        Assert.Equal("4P + 2P", ServingScaler.Describe(sizes, 6));
        Assert.Equal(string.Empty, ServingScaler.Describe(sizes, 3));
    }

    /// <summary>
    /// An ingredient with no quantities scales to null.
    /// </summary>
    [Fact]
    public void Scale_NoQuantities_ReturnsNull()
    {
        Assert.Null(ServingScaler.Scale(new Ingredient { Name = "Salt" }, 2));
    }
}