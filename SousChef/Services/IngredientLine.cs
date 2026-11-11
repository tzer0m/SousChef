using SousChef.Core.Cooking;
using SousChef.Core.Models;

namespace SousChef.Services;

/// <summary>
/// An ingredient with its scaled amount and allergen names, ready to display.
/// </summary>
public class IngredientLine
{
    /// <summary>
    /// The ingredient.
    /// </summary>
    public Ingredient Ingredient { get; set; } = new();

    /// <summary>
    /// The scaled amount, such as "300 g".
    /// </summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// The names of the ingredient's allergens.
    /// </summary>
    public string Allergens { get; set; } = string.Empty;

    /// <summary>
    /// Builds display lines for every ingredient of a recipe.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="servings">The number of people.</param>
    public static List<IngredientLine> Build(Recipe recipe, int servings)
    {
        return [.. recipe.Ingredients.Select(i => new IngredientLine { Ingredient = i, Amount = QuantityFormatter.Format(ServingScaler.Scale(i, servings), i.Unit), Allergens = string.Join(", ", i.AllergenCodes.Select(c => recipe.Allergens.FirstOrDefault(a => a.Code == c)?.Name ?? $"#{c}")) })];
    }
}