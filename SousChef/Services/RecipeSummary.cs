namespace SousChef.Services;

/// <summary>
/// The details shown for a recipe in the library.
/// </summary>
public class RecipeSummary
{
    /// <summary>
    /// The recipe ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The recipe title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The line below the title.
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// The card's tags, separated by "|".
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Whether the card has a spice tag.
    /// </summary>
    public bool IsSpicy { get; set; }

    /// <summary>
    /// The upper bound of the cooking time in minutes.
    /// </summary>
    public int? TimeMaxMinutes { get; set; }

    /// <summary>
    /// Whether the recipe is a favourite.
    /// </summary>
    public bool IsFavourite { get; set; }

    /// <summary>
    /// How many times the recipe has been cooked.
    /// </summary>
    public int TimesCooked { get; set; }

    /// <summary>
    /// Whether the parser reported any warnings.
    /// </summary>
    public bool HasWarnings { get; set; }

    /// <summary>
    /// Whether a hero image was extracted.
    /// </summary>
    public bool HasHero { get; set; }

    /// <summary>
    /// A version number for image URLs, so browsers refetch after a re-parse.
    /// </summary>
    public long Version { get; set; }
}