namespace SousChef.Core.Models;

/// <summary>
/// The output of parsing a recipe card PDF.
/// </summary>
public class ParseResult
{
    /// <summary>
    /// The parsed recipe.
    /// </summary>
    public Recipe Recipe { get; set; } = new();

    /// <summary>
    /// The images extracted from the card.
    /// </summary>
    public List<RecipeImage> Images { get; set; } = [];
}