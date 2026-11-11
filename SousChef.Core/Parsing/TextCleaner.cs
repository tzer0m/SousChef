namespace SousChef.Core.Parsing;

/// <summary>
/// Fixes characters the card's fonts don't map to text properly.
/// </summary>
internal static class TextCleaner
{
    /// <summary>
    /// Restores lost ligatures (the "ft" glyph comes out as a NUL character, e.g. "a\0er").
    /// </summary>
    /// <param name="text">The raw text.</param>
    public static string Clean(string text)
    {
        return text.Replace("\0", "ft");
    }
}