namespace SousChef.Core.Models;

/// <summary>
/// An entry from the card's allergen legend.
/// </summary>
public class Allergen
{
    /// <summary>
    /// The allergen code used in the ingredient table.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// The allergen name, such as "Soya".
    /// </summary>
    public string Name { get; set; } = string.Empty;
}