namespace SousChef.Services;

/// <summary>
/// One ingredient row on the edit form.
/// </summary>
public class IngredientEditRow
{
    /// <summary>
    /// The ingredient name; rows left blank are dropped.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The unit, such as "g" or "sachet(s)".
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Whether the ingredient comes from the pantry.
    /// </summary>
    public bool IsPantry { get; set; }

    /// <summary>
    /// The quantity for each serving size, in the order of the card's serving sizes.
    /// </summary>
    public List<decimal?> Quantities { get; set; } = [];
}