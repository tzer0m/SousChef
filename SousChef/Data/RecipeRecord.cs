namespace SousChef.Data;

/// <summary>
/// A stored recipe card: the original PDF, the parsed recipe and personal data.
/// </summary>
public class RecipeRecord
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The HelloFresh product code, used to spot re-imports.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// The recipe number printed on the card.
    /// </summary>
    public int? Number { get; set; }

    /// <summary>
    /// The recipe title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The line below the title.
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// The menu week printed on the card.
    /// </summary>
    public string? WeekCode { get; set; }

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
    /// Lower-case title, subtitle, tags and ingredient names, for searching.
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// The parsed recipe as JSON.
    /// </summary>
    public string RecipeJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether the parser reported any warnings.
    /// </summary>
    public bool HasWarnings { get; set; }

    /// <summary>
    /// Whether the recipe has been corrected by hand (re-parsing then leaves it alone).
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// The Paperless document the card was synced from, if any.
    /// </summary>
    public int? PaperlessId { get; set; }

    /// <summary>
    /// The uploaded file name.
    /// </summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// The original PDF, kept so cards can be re-parsed when the parser improves.
    /// </summary>
    public byte[] Pdf { get; set; } = [];

    /// <summary>
    /// When the card was first imported (UTC).
    /// </summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// When the card was last imported or re-parsed (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Whether the recipe is a favourite.
    /// </summary>
    public bool IsFavourite { get; set; }

    /// <summary>
    /// Personal notes about the recipe.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// How many times the recipe has been cooked.
    /// </summary>
    public int TimesCooked { get; set; }

    /// <summary>
    /// When the recipe was last cooked (UTC).
    /// </summary>
    public DateTime? LastCookedAt { get; set; }

    /// <summary>
    /// The images extracted from the card.
    /// </summary>
    public List<RecipeImageRecord> Images { get; set; } = [];
}