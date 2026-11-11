namespace SousChef.Core.Models;

/// <summary>
/// A range of characters within a piece of text.
/// </summary>
public class TextRange
{
    /// <summary>
    /// The index of the first character.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// The number of characters.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// The index just past the last character.
    /// </summary>
    public int End => Start + Length;
}