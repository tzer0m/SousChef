namespace SousChef.Core.Models;

/// <summary>
/// A row of the nutrition table.
/// </summary>
public class NutritionValue
{
    /// <summary>
    /// The nutrient name, such as "Protein".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unit, such as "g" or "kJ/kcal".
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// The value per serving, as printed.
    /// </summary>
    public string PerServing { get; set; } = string.Empty;

    /// <summary>
    /// The value per 100g, as printed.
    /// </summary>
    public string Per100g { get; set; } = string.Empty;
}