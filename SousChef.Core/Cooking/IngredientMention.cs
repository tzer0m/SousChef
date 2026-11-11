using SousChef.Core.Models;

namespace SousChef.Core.Cooking;

/// <summary>
/// A place in some text where an ingredient is mentioned.
/// </summary>
public class IngredientMention : TextRange
{
    /// <summary>
    /// The index of the ingredient in the recipe's ingredient list.
    /// </summary>
    public int IngredientIndex { get; set; }
}