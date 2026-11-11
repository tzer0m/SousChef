using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// Scales ingredient quantities to a number of people using the card's own columns.
/// </summary>
public static class ServingScaler
{
    /// <summary>
    /// The fewest people a recipe can be scaled to.
    /// </summary>
    public const int MinServings = 1;

    /// <summary>
    /// The most people a recipe can be scaled to.
    /// </summary>
    public const int MaxServings = 8;

    /// <summary>
    /// Returns the quantity of an ingredient for a number of people.
    /// </summary>
    /// <param name="ingredient">The ingredient.</param>
    /// <param name="servings">The number of people.</param>
    public static decimal? Scale(Ingredient ingredient, int servings)
    {
        if (ingredient.Quantities.Count == 0)
        {
            return null;
        }
        if (ingredient.Quantities.TryGetValue(servings, out decimal exact))
        {
            return exact;
        }
        int smallest = ingredient.Quantities.Keys.Min();
        if (servings < smallest)
        {
            return Math.Round(ingredient.Quantities[smallest] * servings / smallest, 2);
        }
        List<int> combination = GetCombination([.. ingredient.Quantities.Keys], servings);
        if (combination.Count > 0)
        {
            return combination.Sum(s => ingredient.Quantities[s]);
        }
        int largest = ingredient.Quantities.Keys.Max();
        return Math.Round(ingredient.Quantities[largest] * servings / largest, 2);
    }

    /// <summary>
    /// Describes how a serving count is made from the card's columns, such as "2P + 3P".
    /// </summary>
    /// <param name="sizes">The serving sizes on the card.</param>
    /// <param name="servings">The number of people.</param>
    public static string Describe(IReadOnlyCollection<int> sizes, int servings)
    {
        if (sizes.Count == 0 || sizes.Contains(servings))
        {
            return string.Empty;
        }
        int smallest = sizes.Min();
        if (servings < smallest)
        {
            return $"{servings}/{smallest} of the {smallest}P quantities";
        }
        List<int> combination = GetCombination(sizes, servings);
        return combination.Count > 0 ? string.Join(" + ", combination.Select(s => $"{s}P")) : "scaled from the largest size";
    }

    /// <summary>
    /// Finds the fewest card columns that add up to a serving count, preferring larger columns.
    /// </summary>
    /// <param name="sizes">The serving sizes on the card.</param>
    /// <param name="servings">The number of people.</param>
    public static List<int> GetCombination(IReadOnlyCollection<int> sizes, int servings)
    {
        List<int>?[] best = new List<int>?[servings + 1];
        best[0] = [];
        for (int total = 1; total <= servings; total++)
        {
            foreach (int size in sizes.Where(s => s > 0 && s <= total))
            {
                List<int>? rest = best[total - size];
                if (rest == null)
                {
                    continue;
                }
                List<int> candidate = [.. rest.Append(size).OrderDescending()];
                if (best[total] == null || IsBetter(candidate, best[total]!))
                {
                    best[total] = candidate;
                }
            }
        }
        return best[servings] ?? [];
    }

    /// <summary>
    /// Checks whether one combination beats another: fewer parts first, then larger parts.
    /// </summary>
    /// <param name="candidate">The new combination, sorted largest first.</param>
    /// <param name="current">The current best, sorted largest first.</param>
    private static bool IsBetter(List<int> candidate, List<int> current)
    {
        if (candidate.Count != current.Count)
        {
            return candidate.Count < current.Count;
        }
        for (int i = 0; i < candidate.Count; i++)
        {
            if (candidate[i] != current[i])
            {
                return candidate[i] > current[i];
            }
        }
        return false;
    }
}