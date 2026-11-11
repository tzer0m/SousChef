namespace SousChef.Core.Models;

/// <summary>
/// An image extracted from a recipe card.
/// </summary>
public class RecipeImage
{
    /// <summary>
    /// The image key, such as "hero", "step-1" or "ingredient-0".
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The image MIME type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The image bytes.
    /// </summary>
    public byte[] Data { get; set; } = [];
}