namespace SousChef.Core.Models;

/// <summary>
/// A recipe parsed from a HelloFresh recipe card.
/// </summary>
public class Recipe
{
    /// <summary>
    /// The recipe number printed on the card.
    /// </summary>
    public int? Number { get; set; }

    /// <summary>
    /// The HelloFresh product code, taken from the file name (e.g. QR3208-79).
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// The menu week printed in the footer (e.g. 2026-W46).
    /// </summary>
    public string? WeekCode { get; set; }

    /// <summary>
    /// The recipe title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The line below the title, usually starting "with".
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// The coloured tags on the card, such as "Calorie Smart".
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// The spice tag (printed in red), if the recipe has one.
    /// </summary>
    public string? SpiceLevel { get; set; }

    /// <summary>
    /// The lower bound of the total cooking time in minutes.
    /// </summary>
    public int? TimeMinMinutes { get; set; }

    /// <summary>
    /// The upper bound of the total cooking time in minutes.
    /// </summary>
    public int? TimeMaxMinutes { get; set; }

    /// <summary>
    /// Other facts from the tag line, such as "1 of your 5 a day".
    /// </summary>
    public List<string> Extras { get; set; } = [];

    /// <summary>
    /// The serving sizes the card has quantities for.
    /// </summary>
    public List<int> ServingSizes { get; set; } = [];

    /// <summary>
    /// The cooking tools needed.
    /// </summary>
    public List<string> Tools { get; set; } = [];

    /// <summary>
    /// The boxed and pantry ingredients.
    /// </summary>
    public List<Ingredient> Ingredients { get; set; } = [];

    /// <summary>
    /// Basics used in the method but not listed, such as oil and salt.
    /// </summary>
    public List<string> ImpliedPantry { get; set; } = [];

    /// <summary>
    /// The method steps.
    /// </summary>
    public List<RecipeStep> Steps { get; set; } = [];

    /// <summary>
    /// The nutrition table rows.
    /// </summary>
    public List<NutritionValue> Nutrition { get; set; } = [];

    /// <summary>
    /// The note printed under the nutrition table.
    /// </summary>
    public string? NutritionNote { get; set; }

    /// <summary>
    /// The allergens legend.
    /// </summary>
    public List<Allergen> Allergens { get; set; } = [];

    /// <summary>
    /// Whether the recipe was read from a scan's OCR text rather than a downloaded card.
    /// </summary>
    public bool IsScan { get; set; }

    /// <summary>
    /// Problems found while parsing the card.
    /// </summary>
    public List<string> ParseWarnings { get; set; } = [];

    /// <summary>
    /// The key of the hero image, if one was extracted.
    /// </summary>
    public string? HeroImageKey { get; set; }
}