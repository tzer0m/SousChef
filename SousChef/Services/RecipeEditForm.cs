namespace SousChef.Services;

/// <summary>
/// The fields of a recipe that can be corrected by hand.
/// </summary>
public class RecipeEditForm
{
    /// <summary>
    /// The recipe title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The line below the title.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// The shortest total time in minutes.
    /// </summary>
    public int? TimeMinMinutes { get; set; }

    /// <summary>
    /// The longest total time in minutes.
    /// </summary>
    public int? TimeMaxMinutes { get; set; }

    /// <summary>
    /// The tags, comma separated.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// The spice tag, if any.
    /// </summary>
    public string? SpiceLevel { get; set; }

    /// <summary>
    /// The cooking tools, comma separated.
    /// </summary>
    public string? Tools { get; set; }

    /// <summary>
    /// The ingredient rows.
    /// </summary>
    public List<IngredientEditRow> Ingredients { get; set; } = [];

    /// <summary>
    /// The steps.
    /// </summary>
    public List<StepEditRow> Steps { get; set; } = [];
}