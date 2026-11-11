namespace SousChef.Data;

/// <summary>
/// An image extracted from a stored recipe card.
/// </summary>
public class RecipeImageRecord
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The recipe the image belongs to.
    /// </summary>
    public int RecipeRecordId { get; set; }

    /// <summary>
    /// The image key, such as "hero" or "step-1".
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