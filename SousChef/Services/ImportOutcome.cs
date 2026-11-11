namespace SousChef.Services;

/// <summary>
/// The result of importing one recipe card.
/// </summary>
public class ImportOutcome
{
    /// <summary>
    /// The uploaded file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the card was imported.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Whether an existing recipe was updated rather than a new one added.
    /// </summary>
    public bool IsUpdate { get; set; }

    /// <summary>
    /// The stored recipe's ID, if imported.
    /// </summary>
    public int? RecipeId { get; set; }

    /// <summary>
    /// The recipe title, if imported.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The error message, if the import failed.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Problems the parser found.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}