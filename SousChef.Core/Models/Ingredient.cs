namespace SousChef.Core.Models;

/// <summary>
/// An ingredient row from the ingredients or pantry table.
/// </summary>
public class Ingredient
{
    /// <summary>
    /// The ingredient name, without markers or allergen codes.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unit as printed on the card (e.g. g, ml, unit(s), sachet(s), tbsp).
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Whether the ingredient comes from the pantry table.
    /// </summary>
    public bool IsPantry { get; set; }

    /// <summary>
    /// Whether the card marks the ingredient as not included in the box.
    /// </summary>
    public bool NotIncluded { get; set; }

    /// <summary>
    /// Whether the card says to store the ingredient in the fridge.
    /// </summary>
    public bool StoreInFridge { get; set; }

    /// <summary>
    /// Whether the method prints this ingredient in red (hot).
    /// </summary>
    public bool IsHot { get; set; }

    /// <summary>
    /// The allergen codes marked against the ingredient.
    /// </summary>
    public List<int> AllergenCodes { get; set; } = [];

    /// <summary>
    /// The quantity for each serving size on the card.
    /// </summary>
    public Dictionary<int, decimal> Quantities { get; set; } = [];

    /// <summary>
    /// The key of the ingredient's thumbnail image, if one was extracted.
    /// </summary>
    public string? ImageKey { get; set; }
}